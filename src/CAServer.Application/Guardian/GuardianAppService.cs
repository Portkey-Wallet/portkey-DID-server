using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.Guardian;
using CAServer.Guardian.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Guardian;

[RemoteService(false)]
[DisableAuditing]
public class GuardianAppService : CAServerAppService, IGuardianAppService
{
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;
    private readonly INESTRepository<UserExtraInfoIndex, string> _userExtraInfoRepository;
    private readonly ILogger<GuardianAppService> _logger;
    private readonly ChainOptions _chainOptions;
    private readonly IGuardianProvider _guardianProvider;
    private readonly IClusterClient _clusterClient;

    public GuardianAppService(
        INESTRepository<GuardianIndex, string> guardianRepository,
        INESTRepository<UserExtraInfoIndex, string> userExtraInfoRepository,
        ILogger<GuardianAppService> logger,
        IOptions<ChainOptions> chainOptions,
        IGuardianProvider guardianProvider,
        IClusterClient clusterClient)
    {
        _guardianRepository = guardianRepository;
        _userExtraInfoRepository = userExtraInfoRepository;
        _logger = logger;
        _chainOptions = chainOptions.Value;
        _guardianProvider = guardianProvider;
        _clusterClient = clusterClient;
    }

    public async Task<GuardianResultDto> GetGuardianIdentifiersAsync(GuardianIdentifierDto guardianIdentifierDto)
    {
        var hash = await GetHashFromIdentifierAsync(guardianIdentifierDto.GuardianIdentifier);
        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new UserFriendlyException($"{guardianIdentifierDto.GuardianIdentifier} not exist.",
                GuardianMessageCode.NotExist);
        }

        var holderInfo = await GetHolderInfosAsync(hash, guardianIdentifierDto.ChainId, guardianIdentifierDto.CaHash,
            guardianIdentifierDto.GuardianIdentifier);
        var guardianResult =
            ObjectMapper.Map<GetHolderInfoOutput, GuardianResultDto>(holderInfo);

        var identifierHashList = holderInfo.GuardianList.Guardians.Select(t => t.IdentifierHash.ToHex()).ToList();
        var hashDic = await GetIdentifiersAsync(identifierHashList);
        var identifiers = hashDic?.Values?.ToList();

        var userExtraInfos = await GetUserExtraInfoAsync(identifiers);

        if (guardianResult?.GuardianList?.Guardians?.Count == 0)
        {
            throw new UserFriendlyException("This address is already registered on another chain.", "20004");
        }

        AddGuardianInfo(guardianResult?.GuardianList?.Guardians, hashDic, userExtraInfos);

        return guardianResult;
    }

    public async Task<RegisterInfoResultDto> GetRegisterInfoAsync(RegisterInfoDto requestDto)
    {
        var guardianIdentifierHash = GetHash(requestDto.LoginGuardianIdentifier);
        var guardians = await _guardianProvider.GetGuardiansAsync(guardianIdentifierHash, requestDto.CaHash);

        var guardian = guardians?.CaHolderInfo?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.OriginChainId));
        
        var  originChainId = guardian == null
            ? await GetOrigianChainIdAsync(guardianIdentifierHash, requestDto.CaHash)
            : guardian.OriginChainId;
        
        return new RegisterInfoResultDto { OriginChainId = originChainId };
    }

    private string GetHash(string guardianIdentifier)
    {
        if (string.IsNullOrWhiteSpace(guardianIdentifier)) return string.Empty;

        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);

        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var guardianGrainDto = guardianGrain.GetGuardianAsync(guardianIdentifier).Result;
        if (!guardianGrainDto.Success)
        {
            _logger.LogError($"{guardianGrainDto.Message} guardianIdentifier: {guardianIdentifier}");
            throw new UserFriendlyException(guardianGrainDto.Message, GuardianMessageCode.NotExist);
        }

        return guardianGrainDto.Data.IdentifierHash;
    }

    private async Task<string> GetOrigianChainIdAsync(string guardianIdentifierHash, string caHash)
    {
        foreach (var chainInfo in _chainOptions.ChainInfos)
        {
            try
            {
                var holderInfo = await GetHolderInfoFromContractAsync(guardianIdentifierHash, caHash, chainInfo.Value);
                if (holderInfo?.GuardianList?.Guardians?.Count > 0) return chainInfo.Key;
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("Not found ca_hash"))
                {
                    _logger.LogError(e, "GetRegisterHolderInfoAsync: guardian hash call contract GetHolderInfo fail.");
                    throw new UserFriendlyException(e.Message);
                }
            }
        }

        throw new UserFriendlyException("This address is not registered.", GuardianMessageCode.NotExist);
    }

    private void AddGuardianInfo(List<GuardianDto> guardians, Dictionary<string, string> hashDic,
        List<UserExtraInfoIndex> userExtraInfos)
    {
        if (guardians == null || guardians.Count == 0)
        {
            return;
        }

        guardians.ForEach(t =>
        {
            t.GuardianIdentifier = hashDic.GetValueOrDefault(t.IdentifierHash);

            var extraInfo = userExtraInfos?.FirstOrDefault(f => f.Id == t.GuardianIdentifier);
            if (extraInfo != null)
            {
                if (t.Type == GuardianIdentifierType.Google.ToString() ||
                    t.Type == GuardianIdentifierType.Apple.ToString())
                {
                    t.ThirdPartyEmail = extraInfo.Email;
                    t.FirstName = extraInfo.FirstName;
                    t.LastName = extraInfo.LastName;
                }

                if (t.Type == GuardianIdentifierType.Apple.ToString())
                {
                    t.IsPrivate = extraInfo.IsPrivateEmail;
                }
            }
        });
    }

    private async Task<string> GetHashFromIdentifierAsync(string guardianIdentifier)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>() { };

        mustQuery.Add(q => q.Term(i => i.Field(f => f.Identifier).Value(guardianIdentifier)));

        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var guardianGrainDto = await _guardianRepository.GetAsync(Filter);
        return guardianGrainDto?.IdentifierHash;
    }

    private async Task<GetHolderInfoOutput> GetHolderInfosAsync(string guardianIdentifierHash, string chainId,
        string caHash, string guardianIdentifier)
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];
            return await GetHolderInfoFromContractAsync(guardianIdentifierHash, caHash, chainInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "guardian hash call contract GetHolderInfo fail.");
            if (!ex.Message.Contains("Not found ca_hash"))
            {
                throw new UserFriendlyException(ex.Message);
            }

            if (!string.IsNullOrWhiteSpace(caHash))
            {
                throw new UserFriendlyException($"{caHash} not exist.",
                    GuardianMessageCode.NotExist);
            }

            throw new UserFriendlyException($"{guardianIdentifier ?? "identifier"} not exist.",
                GuardianMessageCode.NotExist);
        }
    }

    private async Task<GetHolderInfoOutput> GetHolderInfoFromContractAsync(
        string guardianIdentifierHash,
        string caHash,
        ChainInfo chainInfo)
    {
        var param = new GetHolderInfoInput();

        if (!string.IsNullOrWhiteSpace(caHash))
        {
            param.CaHash = Hash.LoadFromHex(caHash);
            param.LoginGuardianIdentifierHash = null;
        }
        else
        {
            param.LoginGuardianIdentifierHash = AElf.Types.Hash.LoadFromHex(guardianIdentifierHash);
            param.CaHash = null;
        }

        var output =
            await ContractHelper.CallTransactionAsync<GetHolderInfoOutput>(MethodName.GetHolderInfo, param, false,
                chainInfo);

        return output;
    }

    private async Task<Dictionary<string, string>> GetIdentifiersAsync(List<string> identifierHashList)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.IdentifierHash).Terms(identifierHashList)));

        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var guardians = await _guardianRepository.GetListAsync(Filter);

        return guardians.Item2?.ToDictionary(t => t.IdentifierHash, t => t.Identifier);
    }

    private async Task<List<UserExtraInfoIndex>> GetUserExtraInfoAsync(List<string> identifiers)
    {
        if (identifiers == null || identifiers.Count == 0)
        {
            return new List<UserExtraInfoIndex>();
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<UserExtraInfoIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(identifiers)));

        QueryContainer Filter(QueryContainerDescriptor<UserExtraInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var userExtraInfos = await _userExtraInfoRepository.GetListAsync(Filter);

        return userExtraInfos.Item2;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Indexing.Elasticsearch;
using CAServer.AppleAuth.Provider;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain.Guardian;
using CAServer.Guardian.Provider;
using CAServer.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Orleans;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using ChainOptions = CAServer.Grains.Grain.ApplicationHandler.ChainOptions;

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
    private readonly IAppleUserProvider _appleUserProvider;
    private readonly AppleTransferOptions _appleTransferOptions;
    private readonly VerifierIdMappingOptions _verifierIdMappingOptions;
    private readonly IContractProvider _contractProvider;
    private readonly IDistributedCache<string> _distributedCache;
    private const string VerifierMapperCacheKey = "VerifierMapperCacheKey";


    public GuardianAppService(
        INESTRepository<GuardianIndex, string> guardianRepository, IAppleUserProvider appleUserProvider,
        INESTRepository<UserExtraInfoIndex, string> userExtraInfoRepository, ILogger<GuardianAppService> logger,
        IOptions<ChainOptions> chainOptions, IGuardianProvider guardianProvider, IClusterClient clusterClient,
        IOptionsSnapshot<AppleTransferOptions> appleTransferOptions,
        IOptionsSnapshot<VerifierIdMappingOptions> verifierIdMappingOptions,
        IDistributedCache<string> distributedCache, IContractProvider contractProvider)
    {
        _guardianRepository = guardianRepository;
        _userExtraInfoRepository = userExtraInfoRepository;
        _logger = logger;
        _chainOptions = chainOptions.Value;
        _guardianProvider = guardianProvider;
        _clusterClient = clusterClient;
        _distributedCache = distributedCache;
        _contractProvider = contractProvider;
        _verifierIdMappingOptions = verifierIdMappingOptions.Value;
        _appleUserProvider = appleUserProvider;
        _appleTransferOptions = appleTransferOptions.Value;
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
        var guardianDtos = guardianResult.GuardianList.Guardians;
        foreach (var dto in guardianDtos)
        {
            var verifyMap = _verifierIdMappingOptions.VerifierIdMap;
            if (!verifyMap.TryGetValue(dto.VerifierId, out var verifierId))
            {
                continue;
            }
            var result = await GetVerifierServerAsync(dto.VerifierId, guardianIdentifierDto.ChainId);
            if (result)
            {
                dto.VerifierId = verifierId;
            }
        }


        var identifierHashList = holderInfo.GuardianList.Guardians.Select(t => t.IdentifierHash.ToHex()).ToList();
        var hashDic = await GetIdentifiersAsync(identifierHashList);
        var identifiers = hashDic?.Values?.ToList();

        var userExtraInfos = await GetUserExtraInfoAsync(identifiers);

        if (guardianResult?.GuardianList?.Guardians?.Count == 0 ||
            (!guardianResult.CreateChainId.IsNullOrWhiteSpace() &&
             guardianResult.CreateChainId != guardianIdentifierDto.ChainId))
        {
            throw new UserFriendlyException("This address is already registered on another chain.", "20004");
        }

        await AddGuardianInfoAsync(guardianResult?.GuardianList?.Guardians, hashDic, userExtraInfos);

        return guardianResult;
    }

    public async Task<RegisterInfoResultDto> GetRegisterInfoAsync(RegisterInfoDto requestDto)
    {
        if (_appleTransferOptions.IsNeedIntercept(requestDto.LoginGuardianIdentifier))
        {
            throw new UserFriendlyException(_appleTransferOptions.ErrorMessage);
        }

        var guardianIdentifierHash = GetHash(requestDto.LoginGuardianIdentifier);
        var guardians = await _guardianProvider.GetGuardiansAsync(guardianIdentifierHash, requestDto.CaHash);

        var guardian = guardians?.CaHolderInfo?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.OriginChainId));

        var originChainId = guardian == null
            ? await GetOriginChainIdAsync(guardianIdentifierHash, requestDto.CaHash)
            : guardian.OriginChainId;

        return new RegisterInfoResultDto { OriginChainId = originChainId };
    }

    public async Task<List<GuardianIndexDto>> GetGuardianListAsync(List<string> identifierHashList)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.IdentifierHash).Terms(identifierHashList)));
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var guardians = await _guardianRepository.GetListAsync(Filter);

        var result = guardians.Item2.Where(t => t.IsDeleted == false).ToList();

        return ObjectMapper.Map<List<GuardianIndex>, List<GuardianIndexDto>>(result);
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

    private async Task<string> GetOriginChainIdAsync(string guardianIdentifierHash, string caHash)
    {
        foreach (var (chainId, chainInfo) in _chainOptions.ChainInfos)
        {
            try
            {
                var holderInfo =
                    await _guardianProvider.GetHolderInfoFromContractAsync(guardianIdentifierHash, caHash, chainId);
                if (holderInfo.CreateChainId > 0)
                {
                    return ChainHelper.ConvertChainIdToBase58(holderInfo.CreateChainId);
                }

                if (holderInfo?.GuardianList?.Guardians?.Count > 0)
                {
                    return chainId;
                }
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

    private async Task AddGuardianInfoAsync(List<GuardianDto> guardians, Dictionary<string, string> hashDic,
        List<UserExtraInfoIndex> userExtraInfos)
    {
        if (guardians == null || guardians.Count == 0)
        {
            return;
        }

        foreach (var guardian in guardians)
        {
            guardian.GuardianIdentifier = hashDic.GetValueOrDefault(guardian.IdentifierHash);

            var extraInfo = userExtraInfos?.FirstOrDefault(f => f.Id == guardian.GuardianIdentifier);
            if (extraInfo != null)
            {
                guardian.ThirdPartyEmail = extraInfo.Email;
                if (guardian.Type == GuardianIdentifierType.Google.ToString())
                {
                    guardian.FirstName = extraInfo.FirstName;
                    guardian.LastName = extraInfo.LastName;
                }

                if (guardian.Type == GuardianIdentifierType.Apple.ToString())
                {
                    await SetNameAsync(guardian);
                    guardian.IsPrivate = extraInfo.IsPrivateEmail;
                }
            }
        }
    }

    private async Task<string> GetHashFromIdentifierAsync(string guardianIdentifier)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>() { };

        mustQuery.Add(q => q.Term(i => i.Field(f => f.Identifier).Value(guardianIdentifier)));
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var guardianGrainDto = await _guardianRepository.GetAsync(Filter);
        if (guardianGrainDto == null || guardianGrainDto.IsDeleted) return null;

        return guardianGrainDto?.IdentifierHash;
    }

    private async Task<GetHolderInfoOutput> GetHolderInfosAsync(string guardianIdentifierHash, string chainId,
        string caHash, string guardianIdentifier)
    {
        try
        {
            return await _guardianProvider.GetHolderInfoFromContractAsync(guardianIdentifierHash, caHash, chainId);
        }
        catch (Exception ex)
        {
            if (!ex.Message.Contains("Not found ca_hash"))
            {
                _logger.LogError(ex, "{Message}, {Data}", "guardian hash call contract GetHolderInfo fail.",
                    $"guardianIdentifierHash={guardianIdentifierHash ?? ""}, chainId={chainId ?? ""}, caHash={caHash ?? ""}, guardianIdentifier={guardianIdentifier ?? ""}");
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

    private async Task<Dictionary<string, string>> GetIdentifiersAsync(List<string> identifierHashList)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.IdentifierHash).Terms(identifierHashList)));
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var guardians = await _guardianRepository.GetListAsync(Filter);

        var result = guardians.Item2.Where(t => t.IsDeleted == false);

        return result?.ToDictionary(t => t.IdentifierHash, t => t.Identifier);
    }

    private async Task<List<UserExtraInfoIndex>> GetUserExtraInfoAsync(List<string> identifiers)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "in GetUserExtraInfoAsync");
        }

        return new List<UserExtraInfoIndex>();
    }

    private async Task SetNameAsync(GuardianDto guardian)
    {
        var userInfo = await _appleUserProvider.GetUserExtraInfoAsync(guardian.GuardianIdentifier);
        if (userInfo == null) return;

        guardian.FirstName = userInfo.FirstName;
        guardian.LastName = userInfo.LastName;
    }

    private async Task<bool> GetVerifierServerAsync(string verifierId, string chainId)
    {
        var key = string.Join(":", VerifierMapperCacheKey, verifierId);
        var existCacheItem = await _distributedCache.GetAsync(key);
        if (existCacheItem != null)
        {
            return true;
        }

        var list = await _contractProvider.GetVerifierServersListAsync(chainId);

        var serverInfo = list.VerifierServers.FirstOrDefault(t => t.Id.ToHex() == verifierId);

        if (serverInfo != null)
        {
            return false;
        }

        await _distributedCache.SetAsync(key, string.Empty, new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = CommonConstant.DefaultAbsoluteExpiration
        });
        return true;
    }
}
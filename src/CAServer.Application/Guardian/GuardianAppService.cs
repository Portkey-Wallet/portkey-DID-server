using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.LinqToElasticSearch.Provider;
using AElf.Types;
using CAServer.AppleAuth.Provider;
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
    private readonly ILinqRepository<GuardianIndex, string> _guardianRepository;
    private readonly ILinqRepository<UserExtraInfoIndex, string> _userExtraInfoRepository;
    private readonly ILogger<GuardianAppService> _logger;
    private readonly ChainOptions _chainOptions;
    private readonly IGuardianProvider _guardianProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IAppleUserProvider _appleUserProvider;

    public GuardianAppService(
        ILinqRepository<GuardianIndex, string> guardianRepository, IAppleUserProvider appleUserProvider,
        ILinqRepository<UserExtraInfoIndex, string> userExtraInfoRepository, ILogger<GuardianAppService> logger,
        IOptions<ChainOptions> chainOptions, IGuardianProvider guardianProvider, IClusterClient clusterClient)
    {
        _guardianRepository = guardianRepository;
        _userExtraInfoRepository = userExtraInfoRepository;
        _logger = logger;
        _chainOptions = chainOptions.Value;
        _guardianProvider = guardianProvider;
        _clusterClient = clusterClient;
        _appleUserProvider = appleUserProvider;
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

        await AddGuardianInfoAsync(guardianResult?.GuardianList?.Guardians, hashDic, userExtraInfos);

        return guardianResult;
    }

    public async Task<RegisterInfoResultDto> GetRegisterInfoAsync(RegisterInfoDto requestDto)
    {
        var guardianIdentifierHash = GetHash(requestDto.LoginGuardianIdentifier);
        var guardians = await _guardianProvider.GetGuardiansAsync(guardianIdentifierHash, requestDto.CaHash);

        var guardian = guardians?.CaHolderInfo?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.OriginChainId));

        var originChainId = guardian == null
            ? await GetOriginChainIdAsync(guardianIdentifierHash, requestDto.CaHash)
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

    private async Task<string> GetOriginChainIdAsync(string guardianIdentifierHash, string caHash)
    {
        foreach (var (chainId, chainInfo) in _chainOptions.ChainInfos)
        {
            try
            {
                var holderInfo =
                    await _guardianProvider.GetHolderInfoFromContractAsync(guardianIdentifierHash, caHash, chainId);
                if (holderInfo?.GuardianList?.Guardians?.Count > 0) return chainId;
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
        Expression<Func<GuardianIndex, bool>> expression = f => f.Identifier == guardianIdentifier;
        var guardianGrainDto = _guardianRepository.WhereClause(expression).Skip(0).Take(1000).ToList();
        return guardianGrainDto?.FirstOrDefault()?.IdentifierHash;
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
        Expression<Func<GuardianIndex, bool>> expression = f => identifierHashList.Contains(f.IdentifierHash);
        var guardians = _guardianRepository.WhereClause(expression).Skip(0).Take(1000).ToList();
        return guardians?.ToDictionary(t => t.IdentifierHash, t => t.Identifier);
    }

    private async Task<List<UserExtraInfoIndex>> GetUserExtraInfoAsync(List<string> identifiers)
    {
        try
        {
            if (identifiers == null || identifiers.Count == 0)
            {
                return new List<UserExtraInfoIndex>();
            }
            
            Expression<Func<UserExtraInfoIndex, bool>> expression = null;
            foreach (var identifier in identifiers)
            {
                expression = expression is null ? (p => p.Id == identifier) : expression.Or(p => p.Id == identifier);
            }
            var userExtraInfos = _userExtraInfoRepository.WhereClause(expression).ToList();

            return userExtraInfos;
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
}
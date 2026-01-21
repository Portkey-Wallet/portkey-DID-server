using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.AppleAuth.Provider;
using CAServer.CAAccount;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Provider;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.Guardian;
using CAServer.Guardian.Provider;
using CAServer.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Newtonsoft.Json;
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
    private const string GuardianCachePrefix = "Portkey:GuardiansCache:";
    private const string RegisterInfoCachePrefix = "Portkey:RegisterInfoCache:";
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;
    private readonly INESTRepository<UserExtraInfoIndex, string> _userExtraInfoRepository;
    private readonly ILogger<GuardianAppService> _logger;
    private readonly ChainOptions _chainOptions;
    private readonly IGuardianProvider _guardianProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IAppleUserProvider _appleUserProvider;
    private readonly AppleTransferOptions _appleTransferOptions;
    private readonly StopRegisterOptions _stopRegisterOptions;
    private readonly INicknameProvider _nicknameProvider;
    private readonly IZkLoginProvider _zkLoginProvider;
    private readonly IDistributedCache<GuardianResultDto> _guardiansCache;
    private readonly IDistributedCache<RegisterInfoResultDto> _registerInfoCache;
    private readonly LoginCacheOptions _loginCacheOptions;

    public GuardianAppService(
        INESTRepository<GuardianIndex, string> guardianRepository, IAppleUserProvider appleUserProvider,
        INESTRepository<UserExtraInfoIndex, string> userExtraInfoRepository, ILogger<GuardianAppService> logger,
        IOptions<ChainOptions> chainOptions, IGuardianProvider guardianProvider, IClusterClient clusterClient,
        IOptionsSnapshot<AppleTransferOptions> appleTransferOptions,
        IOptionsSnapshot<StopRegisterOptions> stopRegisterOptions,
        INicknameProvider nicknameProvider,
        IZkLoginProvider zkLoginProvider,
        IDistributedCache<GuardianResultDto> guardiansCache,
        IDistributedCache<RegisterInfoResultDto> registerInfoCache,
        IOptions<LoginCacheOptions> loginCacheOptions)
    {
        _guardianRepository = guardianRepository;
        _userExtraInfoRepository = userExtraInfoRepository;
        _logger = logger;
        _chainOptions = chainOptions.Value;
        _guardianProvider = guardianProvider;
        _clusterClient = clusterClient;
        _appleUserProvider = appleUserProvider;
        _appleTransferOptions = appleTransferOptions.Value;
        _stopRegisterOptions = stopRegisterOptions.Value;
        _nicknameProvider = nicknameProvider;
        _zkLoginProvider = zkLoginProvider;
        _guardiansCache = guardiansCache;
        _registerInfoCache = registerInfoCache;
        _loginCacheOptions = loginCacheOptions.Value;
    }

    public async Task<GuardianResultDto> GetGuardianIdentifiersWrapperAsync(GuardianIdentifierDto guardianIdentifierDto)
    {
        if (guardianIdentifierDto.GuardianIdentifier.IsNullOrEmpty() || guardianIdentifierDto.ChainId.IsNullOrEmpty())
        {
            return await GetGuardianIdentifiersAsync(guardianIdentifierDto);
        }

        var key = GetGuardianIdentifiersCacheKey(guardianIdentifierDto.GuardianIdentifier,
            guardianIdentifierDto.ChainId);
        var result = await _guardiansCache.GetAsync(key);
        if (result != null)
        {
            return result;
        }

        result = await GetGuardianIdentifiersAsync(guardianIdentifierDto);
        if (result != null)
        {
            await _guardiansCache.SetAsync(key, result, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTimeOffset.UtcNow.AddSeconds(_loginCacheOptions.GuardianIdentifiersCacheSeconds)
            });
        }

        return result;
    }

    private string GetGuardianIdentifiersCacheKey(string guardianIdentifier, string chainId)
    {
        return GuardianCachePrefix + guardianIdentifier + ":" + chainId;
    }

    public async Task<GuardianResultDto> GetGuardianIdentifiersAsync(GuardianIdentifierDto guardianIdentifierDto)
    {
        var sw = new Stopwatch();
        var guardianIdentifierHash = "";
        if (!guardianIdentifierDto.GuardianIdentifier.IsNullOrWhiteSpace())
        {
            //get GuardianIdentifierHash from es
            //cost 2-5ms, ignore, usually more than 900ms
            sw.Start();
            guardianIdentifierHash = await GetHashFromIdentifierAsync(guardianIdentifierDto.GuardianIdentifier);
            sw.Stop();
            _logger.LogInformation("GetGuardianIdentifiersAsync:GetHashFromIdentifierAsync=>cost:{0}ms",
                sw.ElapsedMilliseconds);
            if (string.IsNullOrWhiteSpace(guardianIdentifierHash))
            {
                throw new UserFriendlyException($"{guardianIdentifierDto.GuardianIdentifier} not exist.",
                    GuardianMessageCode.NotExist);
            }
        }

        //get holderInfo from contract
        //100-1600ms, Key optimization
        sw.Restart();
        var guardianResult = await GetHolderInfosAsync(guardianIdentifierHash, guardianIdentifierDto.ChainId,
            guardianIdentifierDto.CaHash,
            guardianIdentifierDto.GuardianIdentifier);
        sw.Stop();
        _logger.LogInformation("GetGuardianIdentifiersAsync:GetHolderInfosAsync=>cost:{0}ms", sw.ElapsedMilliseconds);
        // var guardianResult =
        //     ObjectMapper.Map<GetHolderInfoOutput, GuardianResultDto>(holderInfo);

        if (guardianResult.GuardianList?.Guardians?.Count == 0 ||
            (!guardianResult.CreateChainId.IsNullOrWhiteSpace() &&
             guardianResult.CreateChainId != guardianIdentifierDto.ChainId))
        {
            throw new UserFriendlyException("This address is already registered on another chain.", "20004");
        }

        var identifierHashList = guardianResult.GuardianList?.Guardians?.Select(t => t.IdentifierHash).ToList();
        //batch get guardianIdentifierHash's relevant Identifier from es
        //cost 2-5ms, ignore
        sw.Restart();
        var hashDic = await GetIdentifiersAsync(identifierHashList);
        sw.Stop();
        _logger.LogInformation("GetGuardianIdentifiersAsync:GetIdentifiersAsync=>cost:{0}ms", sw.ElapsedMilliseconds);
        var identifiers = hashDic?.Values.ToList();
        //get UserExtraInfo from es
        //usually cost 2ms, seldom cost 100-300ms
        sw.Restart();
        var userExtraInfos = await GetUserExtraInfoAsync(identifiers);
        sw.Stop();
        _logger.LogInformation("GetGuardianIdentifiersAsync:GetUserExtraInfoAsync=>cost:{0}ms", sw.ElapsedMilliseconds);
        await AddGuardianInfoAsync(guardianResult.GuardianList?.Guardians, hashDic, userExtraInfos);
        // SetGuardianVerifiedZkField(guardianResult);
        return guardianResult;
    }

    // private void SetGuardianVerifiedZkField(GuardianResultDto guardianResult)
    // {
    //     if (guardianResult.GuardianList is null || guardianResult.GuardianList.Guardians.IsNullOrEmpty())
    //     {
    //         return;
    //     }
    //
    //     foreach (var guardian in guardianResult.GuardianList.Guardians)
    //     {
    //         var zkLoginInfo = guardian.ZkLoginInfo;
    //         guardian.VerifiedByZk = zkLoginInfo is not null
    //                                 && zkLoginInfo.IdentifierHash is not (null or "")
    //                                 && zkLoginInfo.Salt is not (null or "")
    //                                 && zkLoginInfo.Nonce is not (null or "")
    //                                 && zkLoginInfo.ZkProof is not (null or "")
    //                                 && zkLoginInfo.CircuitId is not (null or "")
    //                                 && zkLoginInfo.Issuer is not (null or "")
    //                                 && zkLoginInfo.Kid is not (null or "")
    //                                 && zkLoginInfo.NoncePayload is not null;
    //         guardian.PoseidonIdentifierHash = zkLoginInfo is not null ? zkLoginInfo.PoseidonIdentifierHash : "";
    //     }
    // }

    public async Task<RegisterInfoResultDto> GetRegisterInfoWrapperAsync(RegisterInfoDto requestDto)
    {
        if (requestDto.LoginGuardianIdentifier.IsNullOrEmpty())
        {
            return await GetRegisterInfoAsync(requestDto);
        }

        var key = GetRegisterInfoCacheKey(requestDto.LoginGuardianIdentifier);
        var result = await _registerInfoCache.GetAsync(key);
        if (result != null)
        {
            return result;
        }

        result = await GetRegisterInfoAsync(requestDto);
        if (result != null)
        {
            await _registerInfoCache.SetAsync(key, result, new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(_loginCacheOptions.RegisterCacheSeconds)
            });
        }

        return result;
    }

    private string GetRegisterInfoCacheKey(string guardianIdentifier)
    {
        return RegisterInfoCachePrefix + guardianIdentifier;
    }

    public async Task<RegisterInfoResultDto> GetRegisterInfoAsync(RegisterInfoDto requestDto)
    {
        if (!requestDto.LoginGuardianIdentifier.IsNullOrEmpty() && _appleTransferOptions.IsNeedIntercept(requestDto.LoginGuardianIdentifier))
        {
            throw new UserFriendlyException(_appleTransferOptions.ErrorMessage);
        }

        var guardianIdentifierHash = GetHash(requestDto.LoginGuardianIdentifier);
        string originChainId;
        if (_loginCacheOptions.RegisterInfoParallelModeSwitch)
        {
            originChainId = await GetOriginalChainIdParallelMode(guardianIdentifierHash, requestDto.CaHash);
        }
        else
        {
            var guardians = await _guardianProvider.GetGuardiansAsync(guardianIdentifierHash, requestDto.CaHash);
            var holderInfo = guardians?.CaHolderInfo?.FirstOrDefault(t =>
                t.GuardianList != null && !t.GuardianList.Guardians.IsNullOrEmpty() &&
                !string.IsNullOrWhiteSpace(t.OriginChainId));
            var guardian = holderInfo?.GuardianList?.Guardians?.FirstOrDefault(t =>
                t.IdentifierHash == guardianIdentifierHash && t.IsLoginGuardian == true);
            
            originChainId = guardian == null
                ? await GetOriginChainIdAsync(guardianIdentifierHash, requestDto.CaHash)
                : holderInfo.OriginChainId;
        }

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
            if (_stopRegisterOptions.Open)
            {
                throw new UserFriendlyException(_stopRegisterOptions.Message, GuardianMessageCode.StopRegister);
            }

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
                if (guardianIdentifierHash.IsNullOrEmpty())
                {
                    var holderInfo =
                        await _guardianProvider.GetHolderInfoFromContractAsync(guardianIdentifierHash, caHash, chainId);
                    if (holderInfo.CreateChainId > 0 &&
                        ChainHelper.ConvertChainIdToBase58(holderInfo.CreateChainId) == chainId)
                    {
                        return ChainHelper.ConvertChainIdToBase58(holderInfo.CreateChainId);
                    }

                    if (holderInfo.CreateChainId == 0 && holderInfo?.GuardianList?.Guardians?.Count > 0)
                    {
                        return chainId;
                    }
                }
                else
                {
                    var guardianResult =
                        await _guardianProvider.GetHolderInfoFromCacheAsync(guardianIdentifierHash, chainId, true);
                    if (!guardianResult.CreateChainId.IsNullOrEmpty() && guardianResult.CreateChainId == chainId)
                    {
                        return guardianResult.CreateChainId;
                    }

                    if (guardianResult.CreateChainId.IsNullOrEmpty() &&
                        guardianResult?.GuardianList?.Guardians?.Count > 0)
                    {
                        return chainId;
                    }
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

        if (_stopRegisterOptions.Open)
        {
            throw new UserFriendlyException(_stopRegisterOptions.Message, GuardianMessageCode.StopRegister);
        }

        throw new UserFriendlyException("This address is not registered.", GuardianMessageCode.NotExist);
    }

    private async Task<string> GetOriginalChainIdParallelMode(string guardianIdentifierHash, string caHash)
    {
        var holderInfoTasks = new List<Task<string>>();
        foreach (var (chainId, chainInfo) in _chainOptions.ChainInfos)
        {
            holderInfoTasks.Add(BuildTaskByContract(guardianIdentifierHash, caHash, chainId));
        }

        var originalChainIds = await Task.WhenAll(holderInfoTasks);
        if (originalChainIds.IsNullOrEmpty() || originalChainIds.Where(item => !item.IsNullOrEmpty()).IsNullOrEmpty())
        {
            throw new UserFriendlyException("This address is not registered.", GuardianMessageCode.NotExist);
        }

        return originalChainIds.FirstOrDefault(item => !item.IsNullOrEmpty());
    }

    private async Task<string> BuildTaskByContract(string guardianIdentifierHash, string caHash, string chainId)
    {
        try
        {
            if (!guardianIdentifierHash.IsNullOrEmpty() && !chainId.IsNullOrEmpty())
            {
                var guardianResultDto =
                    await _guardianProvider.GetHolderInfoFromCacheAsync(guardianIdentifierHash: guardianIdentifierHash,
                        chainId: chainId, needCache: true);
                return guardianResultDto.CreateChainId.IsNullOrEmpty() || guardianResultDto.CreateChainId != chainId
                    ? null
                    : guardianResultDto.CreateChainId;
            }
            else
            {
                var holderInfo =
                    await _guardianProvider.GetHolderInfoFromContractAsync(guardianIdentifierHash, caHash, chainId);
                return holderInfo.CreateChainId > 0 &&
                       ChainHelper.ConvertChainIdToBase58(holderInfo.CreateChainId) == chainId
                    ? ChainHelper.ConvertChainIdToBase58(holderInfo.CreateChainId)
                    : null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetHolderInfoFromContract error");
            return null;
        }
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
                var guardianType = Enum.Parse(typeof(GuardianIdentifierType), guardian.Type);
                switch (guardianType)
                {
                    case GuardianIdentifierType.Google:
                        guardian.FirstName = extraInfo.FirstName;
                        guardian.LastName = extraInfo.LastName;
                        break;
                    case GuardianIdentifierType.Telegram:
                        guardian.FirstName = extraInfo.FirstName;
                        guardian.LastName = extraInfo.LastName;
                        guardian.IsPrivate = true;
                        break;
                    case GuardianIdentifierType.Apple:
                        await SetNameAsync(guardian);
                        guardian.IsPrivate = extraInfo.IsPrivateEmail;
                        break;
                    case GuardianIdentifierType.Twitter:
                        guardian.FirstName = extraInfo.FirstName;
                        guardian.IsPrivate = true;
                        break;
                    case GuardianIdentifierType.Facebook:
                        guardian.FirstName = extraInfo.FirstName;
                        guardian.LastName = extraInfo.LastName;
                        guardian.IsPrivate = true;
                        guardian.ImageUrl = extraInfo.Picture;
                        break;
                }
            }
        }
    }


    private async Task<GuardianResultDto> GetHolderInfosAsync(string guardianIdentifierHash, string chainId,
        string caHash, string guardianIdentifier)
    {
        try
        {
            if (!guardianIdentifierHash.IsNullOrEmpty() && !chainId.IsNullOrEmpty())
            {
                return await _guardianProvider.GetHolderInfoFromCacheAsync(guardianIdentifierHash, chainId, true);
            }

            var holderInfo =
                await _guardianProvider.GetHolderInfoFromContractAsync(guardianIdentifierHash, caHash, chainId);
            var guardianResultDto = ObjectMapper.Map<GetHolderInfoOutput, GuardianResultDto>(holderInfo);
            _guardianProvider.AppendZkLoginInfo(holderInfo, guardianResultDto);
            return guardianResultDto;
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
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>
        {
            q => q.Terms(i => i.Field(f => f.IdentifierHash).Terms(identifierHashList))
        };

        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var guardians = await _guardianRepository.GetListAsync(Filter);

        var result = guardians.Item2.Where(t => t.IsDeleted == false);

        return result.ToDictionary(t => t.IdentifierHash, t => t.Identifier);
    }


    private async Task<string> GetHashFromIdentifierAsync(string guardianIdentifier)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.Identifier).Value(guardianIdentifier))
        };

        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var guardianGrainDto = await _guardianRepository.GetAsync(Filter);
        return guardianGrainDto == null || guardianGrainDto.IsDeleted ? null : guardianGrainDto.IdentifierHash;
    }

    private async Task<List<UserExtraInfoIndex>> GetUserExtraInfoAsync(List<string> identifiers)
    {
        try
        {
            if (identifiers == null || identifiers.Count == 0)
            {
                return new List<UserExtraInfoIndex>();
            }

            var mustQuery = new List<Func<QueryContainerDescriptor<UserExtraInfoIndex>, QueryContainer>>
            {
                q => q.Terms(i => i.Field(f => f.Id).Terms(identifiers))
            };

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
        if (userInfo != null)
        {
            guardian.FirstName = userInfo.FirstName;
            guardian.LastName = userInfo.LastName;
        }
    }

    public async Task<bool> UpdateUnsetGuardianIdentifierAsync(UpdateGuardianIdentifierDto updateGuardianIdentifierDto)
    {
        GuardianResultDto guardianResultDto = await GetGuardianIdentifiersAsync(updateGuardianIdentifierDto);
        if (guardianResultDto == null || guardianResultDto.GuardianList == null ||
            guardianResultDto.GuardianList.Guardians.IsNullOrEmpty())
        {
            return false;
        }

        var result = await ModifyNicknameHandler(guardianResultDto, updateGuardianIdentifierDto.UserId,
            updateGuardianIdentifierDto.UnsetGuardianIdentifierHash);
        _logger.LogInformation("UpdateUnsetGuardianIdentifierAsync result is={0}, caHash={1}", result,
            updateGuardianIdentifierDto.CaHash);
        return result;
    }

    private async Task<bool> ModifyNicknameHandler(GuardianResultDto guardianResultDto, Guid userId,
        string unsetGuardianIdentifierHash)
    {
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        var caHolderGrainDto = await grain.GetCaHolder();
        if (!caHolderGrainDto.Success)
        {
            return false;
        }

        if (caHolderGrainDto.Data == null)
        {
            return false;
        }

        var caHolder = caHolderGrainDto.Data;
        var modifiedNickname = caHolder.ModifiedNickname;
        var identifierHashFromGrain = caHolder.IdentifierHash;
        if (modifiedNickname && identifierHashFromGrain.IsNullOrEmpty())
        {
            return false;
        }

        if (identifierHashFromGrain.IsNullOrEmpty() || !identifierHashFromGrain.Equals(unsetGuardianIdentifierHash))
        {
            return false;
        }

        return await _nicknameProvider.ModifyNicknameHandler(guardianResultDto, userId, caHolder);
    }
}
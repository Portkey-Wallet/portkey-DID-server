using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using CAServer.AppleAuth.Provider;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Device;
using CAServer.Dtos;
using CAServer.Etos;
using CAServer.Grains;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.Guardian;
using CAServer.Guardian;
using CAServer.Guardian.Provider;
using CAServer.IpInfo;
using CAServer.Options;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using CAServer.Verifier;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Orleans;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;
using ChainOptions = CAServer.Grains.Grain.ApplicationHandler.ChainOptions;
using Enum = System.Enum;
using GuardianInfo = CAServer.Account.GuardianInfo;

namespace CAServer.CAAccount;

[RemoteService(false)]
[DisableAuditing]
public class CAAccountAppService : CAServerAppService, ICAAccountAppService
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<CAAccountAppService> _logger;
    private readonly IDeviceAppService _deviceAppService;
    private readonly ChainOptions _chainOptions;
    private readonly IContractProvider _contractProvider;
    private readonly IGuardianProvider _guardianProvider;
    private readonly IUserAssetsProvider _userAssetsProvider;
    private readonly ICAAccountProvider _accountProvider;
    private readonly INickNameAppService _caHolderAppService;
    private readonly IAppleAuthProvider _appleAuthProvider;
    private readonly ManagerCountLimitOptions _managerCountLimitOptions;
    private const int MaxResultCount = 10;
    public const string DefaultSymbol = "ELF";
    public const double MinBanlance = 0.05 * 100000000;
    private readonly IVerifierServerClient _verifierServerClient;
    private readonly IIpInfoAppService _ipInfoAppService;
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
    private readonly IVerifierAppService _verifierAppService;

    public CAAccountAppService(IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        ILogger<CAAccountAppService> logger,
        IDeviceAppService deviceAppService,
        IOptions<ChainOptions> chainOptions,
        IGuardianProvider guardianProvider,
        IContractProvider contractProvider,
        IUserAssetsProvider userAssetsProvider,
        ICAAccountProvider accountProvider,
        INickNameAppService caHolderAppService,
        IAppleAuthProvider appleAuthProvider,
        IOptionsSnapshot<ManagerCountLimitOptions> managerCountLimitOptions,
        IVerifierServerClient verifierServerClient,
        IIpInfoAppService ipInfoAppService,
        JwtSecurityTokenHandler jwtSecurityTokenHandler,
        IVerifierAppService verifierAppService)
    {
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _logger = logger;
        _deviceAppService = deviceAppService;
        _contractProvider = contractProvider;
        _guardianProvider = guardianProvider;
        _userAssetsProvider = userAssetsProvider;
        _caHolderAppService = caHolderAppService;
        _accountProvider = accountProvider;
        _appleAuthProvider = appleAuthProvider;
        _verifierServerClient = verifierServerClient;
        _managerCountLimitOptions = managerCountLimitOptions.Value;
        _chainOptions = chainOptions.Value;
        _ipInfoAppService = ipInfoAppService;
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _verifierAppService = verifierAppService;
    }
    
    public async Task<AccountResultDto> RegisterRequestAsync(RegisterRequestDto input)
    {
        //just deal with google guardian, todo apple guardian and facebook guardian
        _logger.LogInformation("RegisterRequest started.......................");
        if (input.Type.Equals(GuardianIdentifierType.Google))
        {
            await _verifierAppService.GenerateGuardianAndUserInfoForGoogleZkLoginAsync(input.AccessToken,
                input.ZkJwtAuthInfo.Salt);
        }
        var guardianGrainDto = GetGuardian(input.LoginGuardianIdentifier);
        _logger.LogInformation("RegisterRequest guardianGrainDto:{0}", JsonConvert.SerializeObject(guardianGrainDto));
        var registerDto = ObjectMapper.Map<RegisterRequestDto, RegisterDto>(input);
        registerDto.GuardianInfo.IdentifierHash = guardianGrainDto.IdentifierHash;
        SetZkLoginParams(input, registerDto, guardianGrainDto.IdentifierHash);

        _logger.LogInformation($"register dto :{JsonConvert.SerializeObject(registerDto)}");

        var grainId = GrainIdHelper.GenerateGrainId(guardianGrainDto.IdentifierHash, input.VerifierId, input.ChainId,
            input.Manager);

        registerDto.ManagerInfo.ExtraData =
            await _deviceAppService.EncryptExtraDataAsync(registerDto.ManagerInfo.ExtraData, grainId);

        var grain = _clusterClient.GetGrain<IRegisterGrain>(grainId);
        var result = await grain.RequestAsync(ObjectMapper.Map<RegisterDto, RegisterGrainDto>(registerDto));

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        var registerCreateEto = ObjectMapper.Map<RegisterGrainDto, AccountRegisterCreateEto>(result.Data);
        registerCreateEto.IpAddress = _ipInfoAppService.GetRemoteIp();
        await _distributedEventBus.PublishAsync(registerCreateEto);
        return new AccountResultDto(registerDto.Id.ToString());
    }

    private void SetZkLoginParams(RegisterRequestDto input, RegisterDto registerDto, string identifierHash)
    {
        if (input.ZkJwtAuthInfo == null)
        {
            SetDefaultZkJwtAuthInfo(registerDto.GuardianInfo);
        }
        else
        {
            var current = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DoSetZkJwtAuthInfo(input.ZkJwtAuthInfo.Jwt, input.ZkJwtAuthInfo.Nonce, input.ZkJwtAuthInfo.ZkProof,
                input.ZkJwtAuthInfo.Salt, input.ZkJwtAuthInfo.CircuitId,
                input.Manager, identifierHash, current, registerDto.GuardianInfo);
        }
    }

    private void DoSetZkJwtAuthInfo(string jwt, string nonce, string zkProof, string salt, string circuitId, string manager, string identifierHash, long current, GuardianInfo guardianInfo)
    {
        guardianInfo.ZkJwtAuthInfo.IdentifierHash = identifierHash;
        var jwtToken = _jwtSecurityTokenHandler.ReadJwtToken(jwt);
        guardianInfo.ZkJwtAuthInfo.Issuer = jwtToken.Payload.Iss; //jwtToken.Issuer;
        guardianInfo.ZkJwtAuthInfo.Kid = jwtToken.Header.Kid;
        guardianInfo.ZkJwtAuthInfo.Nonce = nonce;
        guardianInfo.ZkJwtAuthInfo.ZkProof = zkProof;
        guardianInfo.ZkJwtAuthInfo.Salt = salt;
        guardianInfo.ZkJwtAuthInfo.CircuitId = circuitId;
        guardianInfo.ZkJwtAuthInfo.NoncePayload.AddManager.IdentifierHash = identifierHash;
        guardianInfo.ZkJwtAuthInfo.NoncePayload.AddManager.ManagerAddress = manager;
        guardianInfo.ZkJwtAuthInfo.NoncePayload.AddManager.Timestamp = current;
    }

    private static void SetDefaultZkJwtAuthInfo(GuardianInfo guardianInfo)
    {
        guardianInfo.ZkJwtAuthInfo.IdentifierHash = "";
        guardianInfo.ZkJwtAuthInfo.Issuer = "";
        guardianInfo.ZkJwtAuthInfo.Kid = "";
        guardianInfo.ZkJwtAuthInfo.Nonce = "";
        guardianInfo.ZkJwtAuthInfo.ZkProof = "";
        guardianInfo.ZkJwtAuthInfo.Salt = "";
        guardianInfo.ZkJwtAuthInfo.CircuitId = "";
        guardianInfo.ZkJwtAuthInfo.NoncePayload.AddManager.IdentifierHash = "";
        guardianInfo.ZkJwtAuthInfo.NoncePayload.AddManager.ManagerAddress = "";
        guardianInfo.ZkJwtAuthInfo.NoncePayload.AddManager.Timestamp = 0;
    }

    private GuardianGrainDto GetGuardian(string guardianIdentifier)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);

        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var guardianGrainDto = guardianGrain.GetGuardianAsync(guardianIdentifier).Result;
        if (!guardianGrainDto.Success)
        {
            _logger.LogError($"{guardianGrainDto.Message} guardianIdentifier: {guardianIdentifier}");
            throw new UserFriendlyException(guardianGrainDto.Message);
        }

        return guardianGrainDto.Data;
    }

    public async Task<AccountResultDto> RecoverRequestAsync(RecoveryRequestDto input)
    {
        var guardianGrainDto = GetGuardian(input.LoginGuardianIdentifier);
        var recoveryDto = ObjectMapper.Map<RecoveryRequestDto, RecoveryDto>(input);
        SetRecoveryZkLoginParams(input, recoveryDto);
        recoveryDto.LoginGuardianIdentifierHash = guardianGrainDto.IdentifierHash;
        if (string.IsNullOrWhiteSpace(recoveryDto.LoginGuardianIdentifierHash))
        {
            _logger.LogError("why recovery LoginGuardianIdentifierHash is null? " +
                             JsonConvert.SerializeObject(guardianGrainDto));
        }

        var dic = input.GuardiansApproved.ToDictionary(k => k.VerificationDoc, v => v.Identifier);
        recoveryDto.GuardianApproved?.ForEach(t =>
        {
            if (dic.TryGetValue(t.VerificationInfo.VerificationDoc, out var identifier) &&
                !string.IsNullOrWhiteSpace(identifier))
            {
                var guardianGrain = GetGuardian(identifier);
                t.IdentifierHash = guardianGrain.IdentifierHash;
            }
        });

        _logger.LogInformation($"recover dto :{JsonConvert.SerializeObject(recoveryDto)}");

        var grainId = GrainIdHelper.GenerateGrainId(guardianGrainDto.IdentifierHash, input.ChainId, input.Manager);

        var grain = _clusterClient.GetGrain<IRecoveryGrain>(grainId);

        var result = await grain.RequestAsync(ObjectMapper.Map<RecoveryDto, RecoveryGrainDto>(recoveryDto));
        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        var caHash = await GetCAHashAsync(input.ChainId, guardianGrainDto.IdentifierHash);

        if (caHash != null)
        {
            result.Data.ManagerInfo.ExtraData =
                await _deviceAppService.EncryptExtraDataAsync(result.Data.ManagerInfo.ExtraData, caHash);
        }

        var recoverCreateEto = ObjectMapper.Map<RecoveryGrainDto, AccountRecoverCreateEto>(result.Data);
        recoverCreateEto.IpAddress = _ipInfoAppService.GetRemoteIp();
        await _distributedEventBus.PublishAsync(recoverCreateEto);

        return new AccountResultDto(recoveryDto.Id.ToString());
    }

    private void SetRecoveryZkLoginParams(RecoveryRequestDto input, RecoveryDto recoveryDto)
    {
        var current = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        foreach (var recoveryGuardian in input.GuardiansApproved)
        {
            var guardianInfo = recoveryDto.GuardianApproved.FirstOrDefault(guardian =>
                guardian.IdentifierHash.Equals(recoveryGuardian.Identifier)
                && ((int)guardian.Type) == ((int)recoveryGuardian.Type));
            if (guardianInfo == null)
            {
                _logger.LogWarning("recoveryGuardian:{0} not exist in RecoveryDto", JsonConvert.SerializeObject(recoveryGuardian));
                continue;
            }
            if (recoveryGuardian.ZkJwtAuthInfo != null)
            {
                DoSetZkJwtAuthInfo(recoveryGuardian.ZkJwtAuthInfo.Jwt, recoveryGuardian.ZkJwtAuthInfo.Nonce,
                    recoveryGuardian.ZkJwtAuthInfo.ZkProof, recoveryGuardian.ZkJwtAuthInfo.Salt,
                    recoveryGuardian.ZkJwtAuthInfo.CircuitId, input.Manager, guardianInfo.IdentifierHash, current, guardianInfo);
            } else
            {
                SetDefaultZkJwtAuthInfo(guardianInfo);
            }
        }
    }

    public async Task<RevokeEntranceResultDto> RevokeEntranceAsync()
    {
        var resultDto = new RevokeEntranceResultDto();

        var caHolder = await _userAssetsProvider.GetCaHolderIndexAsync(CurrentUser.GetId());
        var holderInfo = await _guardianProvider.GetGuardiansAsync(null, caHolder.CaHash);
        var guardianInfo = holderInfo.CaHolderInfo.FirstOrDefault(g => g.GuardianList != null
                                                                       && g.GuardianList.Guardians.Count > 0);
        if (guardianInfo == null)
        {
            resultDto.EntranceDisplay = false;
            return resultDto;
        }

        var loginGuardians = guardianInfo.GuardianList.Guardians.Where(g => g.IsLoginGuardian).ToList();
        resultDto.EntranceDisplay = loginGuardians.Count == 1;
        return resultDto;
    }

    public async Task<CancelCheckResultDto> RevokeCheckAsync(Guid uid)
    {
        var caHolderIndex = await _userAssetsProvider.GetCaHolderIndexAsync(uid);
        var caHash = caHolderIndex.CaHash;
        var caAddressInfos = new List<CAAddressInfo>();
        foreach (var chainId in _chainOptions.ChainInfos.Select(key => _chainOptions.ChainInfos[key.Key])
                     .Select(chainOptionsChainInfo => chainOptionsChainInfo.ChainId))
        {
            try
            {
                var result = await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(caHash), null, chainId);
                if (result != null)
                {
                    caAddressInfos.Add(new CAAddressInfo
                    {
                        CaAddress = result.CaAddress.ToBase58(),
                        ChainId = chainId
                    });
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "get holder from chain error, userId:{userId}, caHash:{caHash}", uid.ToString(),
                    caHash);
                continue;
            }
        }

        var validateAssets = true;
        var tokenRes = await _userAssetsProvider.GetUserTokenInfoAsync(caAddressInfos, "",
            0, MaxResultCount);

        if (tokenRes.CaHolderTokenBalanceInfo?.Data.Count > 0)
        {
            var tokenInfos = tokenRes.CaHolderTokenBalanceInfo.Data
                .Where(o => o.TokenInfo.Symbol == DefaultSymbol && o.Balance >= MinBanlance).ToList();
            if (tokenInfos.Count > 0)
            {
                validateAssets = false;
            }
        }

        var res = await _userAssetsProvider.GetUserNftInfoAsync(caAddressInfos,
            null, 0, MaxResultCount);
        if (res.CaHolderNFTBalanceInfo?.Data.Count > 0)
        {
            validateAssets = false;
        }

        var validateDevice = true;
        var caAddresses = caAddressInfos.Select(t => t.CaAddress).ToList();
        var caHolderManagerInfo = await _userAssetsProvider.GetCaHolderManagerInfoAsync(caAddresses);
        if (caHolderManagerInfo != null && caHolderManagerInfo.CaHolderManagerInfo?.Count > 0)
        {
            var originChainId = caHolderManagerInfo.CaHolderManagerInfo.FirstOrDefault()?.OriginChainId;
            foreach (var caHolderManager in caHolderManagerInfo.CaHolderManagerInfo
                         .Where(caHolderManager => caHolderManager.OriginChainId == originChainId)
                         .Where(caHolderManager => caHolderManager.ManagerInfos.Count > 1))
            {
                validateDevice = false;
            }
        }

        var validateGuardian = true;
        var appleLoginGuardians = await GetGuardianAsync(caHash);
        if (appleLoginGuardians is not { Count: 1 })
        {
            throw new Exception(ResponseMessage.AppleLoginGuardiansExceed);
        }

        var guardian = await _accountProvider.GetIdentifiersAsync(appleLoginGuardians.First().IdentifierHash);

        var caHolderDto =
            await _accountProvider.GetGuardianAddedCAHolderAsync(guardian.IdentifierHash, 0, MaxResultCount);
        if (caHolderDto.GuardianAddedCAHolderInfo?.Data.Count > 1)
        {
            validateGuardian = false;
        }

        return new CancelCheckResultDto()
        {
            ValidatedDevice = validateDevice,
            ValidatedAssets = validateAssets,
            ValidatedGuardian = validateGuardian
        };
    }

    public async Task<RevokeResultDto> RevokeAsync(RevokeDto input)
    {
        Logger.LogInformation("user revoke, apple token: {token}", input.AppleToken);
        var validateResult = await RevokeCheckAsync(CurrentUser.GetId());
        if (!validateResult.ValidatedDevice || !validateResult.ValidatedAssets || !validateResult.ValidatedGuardian)
        {
            Logger.LogInformation(
                "{message}, validateDevice:{validateDevice},validatedAssets:{validatedAssets},validateGuardian{validateGuardian}",
                ResponseMessage.ValidFail, validateResult.ValidatedDevice, validateResult.ValidatedAssets,
                validateResult.ValidatedGuardian);

            throw new UserFriendlyException(ResponseMessage.ValidFail);
        }

        var caHolder = await _userAssetsProvider.GetCaHolderIndexAsync(CurrentUser.GetId());
        if (caHolder.IsDeleted)
        {
            throw new UserFriendlyException(ResponseMessage.AlreadyDeleted);
        }

        var appleLoginGuardians = await GetGuardianAsync(caHolder.CaHash);
        if (appleLoginGuardians?.Count != 1)
        {
            throw new UserFriendlyException(ResponseMessage.AppleLoginGuardiansExceed);
        }

        var guardian = await _accountProvider.GetIdentifiersAsync(appleLoginGuardians.First().IdentifierHash);
        if (guardian == null)
        {
            throw new UserFriendlyException("guardian not exist.");
        }

        var verifyResult = await _appleAuthProvider.VerifyAppleId(input.AppleToken, guardian.Identifier);

        if (!verifyResult)
        {
            throw new UserFriendlyException(ResponseMessage.AppleIdVerifyFail);
        }

        var revokeResult = await _appleAuthProvider.RevokeAsync(input.AppleToken);

        if (revokeResult)
        {
            await DeleteGuardianAsync(guardian.Identifier);
            await _caHolderAppService.DeleteAsync();
            Logger.LogInformation("user revoke success, apple token: {token}", input.AppleToken);
        }

        return new RevokeResultDto()
        {
            Success = revokeResult
        };
    }

    public async Task<AuthorizeDelegateResultDto> AuthorizeDelegateAsync(AssignProjectDelegateeRequestDto input)
    {
        Logger.LogInformation("Authorize Delegate : param is {input}", JsonConvert.SerializeObject(input));
        var assignProjectDelegateeDto =
            ObjectMapper.Map<AssignProjectDelegateeRequestDto, AssignProjectDelegateeDto>(input);
        var transactionResult = await _contractProvider.AuthorizeDelegateAsync(assignProjectDelegateeDto);
        return new AuthorizeDelegateResultDto
        {
            Success = string.IsNullOrWhiteSpace(transactionResult.Error)
        };
    }

    public async Task<RevokeResultDto> RevokeAccountAsync(RevokeAccountInput input)
    {
        var validateResult = await RevokeValidateAsync(CurrentUser.GetId(), input.Type);
        if (!validateResult.ValidatedDevice || !validateResult.ValidatedAssets || !validateResult.ValidatedGuardian)
        {
            Logger.LogInformation(
                "{message}, validateDevice:{validateDevice},validatedAssets:{validatedAssets},validateGuardian{validateGuardian}",
                ResponseMessage.ValidFail, validateResult.ValidatedDevice, validateResult.ValidatedAssets,
                validateResult.ValidatedGuardian);

            throw new UserFriendlyException(ResponseMessage.ValidFail);
        }

        var revokeCodeInput = new VerifyRevokeCodeInput
        {
            VerifierId = input.VerifierId,
            VerifierSessionId = input.VerifierSessionId,
            Type = input.Type,
            GuardianIdentifier = input.GuardianIdentifier,
            VerifyCode = input.Token,
            ChainId = input.ChainId
        };
        try
        {
            var verifyRevokeToken = await _verifierServerClient.VerifyRevokeCodeAsync(revokeCodeInput);
            if (verifyRevokeToken)
            {
                await DeleteGuardianAsync(input.GuardianIdentifier);
                await _caHolderAppService.DeleteAsync();
            }

            return new RevokeResultDto
            {
                Success = verifyRevokeToken
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Revoke token failed:{error}", e.Message);
            return new RevokeResultDto
            {
                Success = false
            };
        }
    }

    public async Task<CancelCheckResultDto> RevokeValidateAsync(Guid userId, string type)
    {
        var caHolderIndex = await _userAssetsProvider.GetCaHolderIndexAsync(userId);
        if (caHolderIndex.IsDeleted)
        {
            throw new UserFriendlyException(ResponseMessage.AlreadyDeleted);
        }

        var caHash = caHolderIndex.CaHash;
        var caAddressInfos = new List<CAAddressInfo>();
        var caHolderDic = new Dictionary<string, GetHolderInfoOutput>();
        var originChainId = 0;
        foreach (var chainId in _chainOptions.ChainInfos.Select(key => _chainOptions.ChainInfos[key.Key])
                     .Select(chainOptionsChainInfo => chainOptionsChainInfo.ChainId))
        {
            try
            {
                var result = await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(caHash), null, chainId);
                if (result == null)
                {
                    continue;
                }

                caHolderDic.Add(chainId, result);
                if (result.CreateChainId > 0)
                {
                    originChainId = result.CreateChainId;
                }

                caAddressInfos.Add(new CAAddressInfo
                {
                    CaAddress = result.CaAddress.ToBase58(),
                    ChainId = chainId
                });
            }
            catch (Exception e)
            {
                Logger.LogError(e, "get holder from chain error, userId:{userId}, caHash:{caHash}", userId.ToString(),
                    caHash);
            }
        }

        var validateAssets = await ValidateAssertsAsync(caAddressInfos);

        var validateDevice = true;
        var chainIdBase58 = ChainHelper.ConvertChainIdToBase58(originChainId);
        caHolderDic.TryGetValue(chainIdBase58, out var holderInfo);
        if (holderInfo != null && holderInfo.ManagerInfos.Count > 1)
        {
            validateDevice = false;
        }
        var validateGuardian = await ValidateGuardianAsync(holderInfo,type);
        return new CancelCheckResultDto
        {
            ValidatedDevice = validateDevice,
            ValidatedAssets = validateAssets,
            ValidatedGuardian = validateGuardian,
        };
    }

    public async Task<CheckManagerCountResultDto> CheckManagerCountAsync(string caHash)
    {
        var guardiansDto = await _guardianProvider.GetGuardiansAsync(null, caHash);
        if (guardiansDto.CaHolderInfo.Count == 0)
        {
            throw new UserFriendlyException("CAHolder is not exist.");
        }

        var guardianDto = guardiansDto.CaHolderInfo.FirstOrDefault();
        _logger.LogInformation("Current manager count: {count}，Limit count is {Limitcount}",
            guardianDto?.ManagerInfos.Count, _managerCountLimitOptions.Limit);
        var checkManagerCount = guardianDto?.ManagerInfos.Count >= _managerCountLimitOptions.Limit;
        return new CheckManagerCountResultDto
        {
            ManagersTooMany = checkManagerCount
        };
    }

    private async Task<bool> ValidateAssertsAsync(List<CAAddressInfo> caAddressInfos)
    {
        var validateAssets = true;
        var tokenRes = await _userAssetsProvider.GetUserTokenInfoAsync(caAddressInfos, DefaultSymbol,
            0, MaxResultCount);

        if (tokenRes.CaHolderTokenBalanceInfo.Data.Count > 0)
        {
            var tokenInfos = tokenRes.CaHolderTokenBalanceInfo.Data
                .Where(o => o.Balance >= MinBanlance).ToList();
            if (tokenInfos.Count > 0)
            {
                validateAssets = false;
            }
        }

        var res = await _userAssetsProvider.GetUserNftInfoAsync(caAddressInfos,
            null, 0, MaxResultCount);
        if (res.CaHolderNFTBalanceInfo.Data.Count > 0)
        {
            validateAssets = false;
        }

        return validateAssets;
    }

    private async Task<bool> ValidateGuardianAsync(GetHolderInfoOutput holderInfo, string type)
    {
        var validateGuardian = true;
        if (holderInfo != null)
        {
            var guardians = holderInfo.GuardianList.Guardians.Where(t => t.IsLoginGuardian).ToList();
            if (guardians.Count > 1)
            {
                validateGuardian = false;
            }

            var value = (int)(GuardianIdentifierType)Enum.Parse(typeof(GuardianIdentifierType), type);
            var guardian = guardians.FirstOrDefault(t=>(int)t.Type == value);
            if (guardian == null)
            {
                throw new Exception(ResponseMessage.LoginGuardianNotExists);
            }
        }

        

        var currentGuardian =
            holderInfo?.GuardianList.Guardians.FirstOrDefault(t => t.IsLoginGuardian && (int)t.Type == (int)(GuardianIdentifierType)Enum.Parse(typeof(GuardianIdentifierType), type));
        if (currentGuardian != null)
        {
            var caHolderDto =
                await _accountProvider.GetGuardianAddedCAHolderAsync(currentGuardian.IdentifierHash.ToHex(), 0,
                    MaxResultCount);
            var tasks = caHolderDto.GuardianAddedCAHolderInfo.Data.Select(
                t => _userAssetsProvider.GetCaHolderIndexByCahashAsync(t.CaHash));
            await tasks.WhenAll();
            if (tasks.Count(t =>!t.Result.IsDeleted) > 1)
            {
                validateGuardian = false;
            }
        }

        return validateGuardian;
    }

    private async Task<List<GuardianInfoBase>> GetGuardianAsync(string caHash)
    {
        var holderInfo = await _guardianProvider.GetGuardiansAsync(null, caHash);

        var guardianInfo = holderInfo.CaHolderInfo.FirstOrDefault(g => g.GuardianList != null
                                                                       && g.GuardianList.Guardians.Count > 0
                                                                       && g.OriginChainId == g.ChainId);

        return guardianInfo?.GuardianList.Guardians
            .Where(t => t.Type.Equals(((int)GuardianIdentifierType.Apple).ToString()) && t.IsLoginGuardian).ToList();
    }

    private async Task<GuardianGrainDto> DeleteGuardianAsync(string guardianIdentifier)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);

        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var guardianGrainDto = await guardianGrain.DeleteGuardian();
        if (!guardianGrainDto.Success)
        {
            _logger.LogError($"{guardianGrainDto.Message} guardianIdentifier: {guardianIdentifier}");
            throw new UserFriendlyException(guardianGrainDto.Message);
        }

        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<GuardianGrainDto, GuardianDeleteEto>(guardianGrainDto.Data));

        Logger.LogInformation("guardian delete success, guardianIdentifier:{guardianIdentifier}", guardianIdentifier);

        return guardianGrainDto.Data;
    }

    private async Task<string> GetCAHashAsync(string chainId, string loginGuardianIdentifierHash)
    {
        var output =
            await _contractProvider.GetHolderInfoAsync(null, Hash.LoadFromHex(loginGuardianIdentifierHash),
                chainId);

        return output?.CaHash?.ToHex();
    }
}
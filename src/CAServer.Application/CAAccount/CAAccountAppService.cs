using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.AppleAuth.Provider;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Device;
using CAServer.Dtos;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.Guardian;
using CAServer.Guardian;
using CAServer.Guardian.Provider;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using CAServer.UserBehavior;
using CAServer.UserBehavior.Etos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.CAAccount;

[RemoteService(false)]
[DisableAuditing]
public class CAAccountAppService : CAServerAppService, ICAAccountAppService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
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
    private const int MaxResultCount = 10;
    public const string DefaultSymbol = "ELF";
    public const double MinBanlance = 0.05 * 100000000;

    public CAAccountAppService(IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        ILogger<CAAccountAppService> logger, IDeviceAppService deviceAppService, IOptions<ChainOptions> chainOptions,
        IGuardianAppService guardianAppService,
        IGuardianProvider guardianProvider,
        IContractProvider contractProvider, IUserAssetsAppService userAssetsAppService,
        IUserAssetsProvider userAssetsProvider,
        ICAAccountProvider accountProvider,
        INickNameAppService caHolderAppService,
        IAppleAuthProvider appleAuthProvider,
        IHttpContextAccessor httpContextAccessor
    )
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
        _chainOptions = chainOptions.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AccountResultDto> RegisterRequestAsync(RegisterRequestDto input)
    {
        var guardianGrainDto = GetGuardian(input.LoginGuardianIdentifier);
        var registerDto = ObjectMapper.Map<RegisterRequestDto, RegisterDto>(input);
        registerDto.GuardianInfo.IdentifierHash = guardianGrainDto.IdentifierHash;

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

        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<RegisterGrainDto, AccountRegisterCreateEto>(result.Data));
        return new AccountResultDto(registerDto.Id.ToString());
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

        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<RecoveryGrainDto, AccountRecoverCreateEto>(result.Data));

        return new AccountResultDto(recoveryDto.Id.ToString());
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
        var appleLoginGuardians = loginGuardians
            .Where(g => g.Type.Equals(((int)GuardianIdentifierType.Apple).ToString())).ToList();
        resultDto.EntranceDisplay = appleLoginGuardians.Count == 1 && loginGuardians.Count == 1;

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

        if (tokenRes.CaHolderTokenBalanceInfo.Data.Count > 0)
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
        if (res.CaHolderNFTBalanceInfo.Data.Count > 0)
        {
            validateAssets = false;
        }

        var validateDevice = true;
        var caAddresses = caAddressInfos.Select(t => t.CaAddress).ToList();
        var caHolderManagerInfo = await _userAssetsProvider.GetCaHolderManagerInfoAsync(caAddresses);
        if (caHolderManagerInfo != null && caHolderManagerInfo.CaHolderManagerInfo.Count > 0)
        {
            var originChainId = caHolderManagerInfo.CaHolderManagerInfo.First().OriginChainId;
            foreach (var caHolderManager in caHolderManagerInfo.CaHolderManagerInfo
                         .Where(caHolderManager => caHolderManager.OriginChainId == originChainId)
                         .Where(caHolderManager => caHolderManager.ManagerInfos.Count > 1))
            {
                validateDevice = false;
            }
        }

        var validateGuardian = true;
        var appleLoginGuardians = await GetGuardianAsync(caHash);
        if (appleLoginGuardians == null && appleLoginGuardians.Count != 1)
        {
            throw new Exception(ResponseMessage.AppleLoginGuardiansExceed);
        }

        var guardian = await _accountProvider.GetIdentifiersAsync(appleLoginGuardians.First().IdentifierHash);

        var caHolderDto =
            await _accountProvider.GetGuardianAddedCAHolderAsync(guardian.IdentifierHash, 0, MaxResultCount);
        if (caHolderDto.GuardianAddedCAHolderInfo.Data.Count > 1)
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

    private async Task<List<GuardianInfoBase>> GetGuardianAsync(string caHash)
    {
        var holderInfo = await _guardianProvider.GetGuardiansAsync(null, caHash);

        var guardianInfo = holderInfo.CaHolderInfo.FirstOrDefault(g => g.GuardianList != null
                                                                       && g.GuardianList.Guardians.Count > 0);

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
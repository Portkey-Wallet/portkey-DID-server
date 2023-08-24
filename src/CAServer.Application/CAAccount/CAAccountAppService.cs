using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Device;
using CAServer.Dtos;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.Guardian;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;

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
    private readonly IUserAssetsAppService _userAssetsAppService;
    private readonly IUserAssetsProvider _userAssetsProvider;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderIndexRepository;
    private readonly INickNameAppService _caHolderAppService;

    public CAAccountAppService(IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        ILogger<CAAccountAppService> logger, IDeviceAppService deviceAppService, IOptions<ChainOptions> chainOptions,
        IContractProvider contractProvider, IUserAssetsAppService userAssetsAppService,
        IUserAssetsProvider userAssetsProvider, INESTRepository<CAHolderIndex, Guid> caHolderIndexRepository,
        INickNameAppService caHolderAppService)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _logger = logger;
        _deviceAppService = deviceAppService;
        _chainOptions = chainOptions.Value;
        _contractProvider = contractProvider;
        _userAssetsAppService = userAssetsAppService;
        _userAssetsProvider = userAssetsProvider;
        _caHolderIndexRepository = caHolderIndexRepository;
        _caHolderAppService = caHolderAppService;
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

    public async Task<CancelCheckResultDto> CancelEntranceAsync()
    {
        //调用check guardian
        return null;
    }

    public async Task<CancelCheckResultDto> CancelCheckAsync(Guid uid)
    {
        var caHolderIndexAsync = await _userAssetsProvider.GetCaHolderIndexAsync(uid);
        var caHash = caHolderIndexAsync.CaHash;
        var caAddressInfos = new List<CAAddressInfo>();
        foreach (var chainId in _chainOptions.ChainInfos.Select(key => _chainOptions.ChainInfos[key.Key]).Select(chainOptionsChainInfo => chainOptionsChainInfo.ChainId))
        {
            var result = await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(caHash), null, chainId);
            if (result != null)
            {
                caAddressInfos.Add(new CAAddressInfo
                {
                    CaAddress = result.CaAddress.ToString(),
                    ChainId = chainId
                });
            }
        }

        var valedateAsset = false;
        var tokenRes = await _userAssetsProvider.GetUserTokenInfoAsync(caAddressInfos, "",
            0, 100);

        if (tokenRes.CaHolderTokenBalanceInfo.Data.Count > 0)
        {
            var tokenInfos = tokenRes.CaHolderTokenBalanceInfo.Data
                .Where(o => o.TokenInfo.Symbol == "ELF" && o.Balance >= 0.05).ToList();
            if (tokenInfos.Count > 0)
            {
                valedateAsset = true;
            }
        }

        var res = await _userAssetsProvider.GetUserNftInfoAsync(caAddressInfos,
            null, 0, 100);
        if (res.CaHolderNFTBalanceInfo.Data.Count > 0)
        {
            valedateAsset = true;
        }

        //check tokens    
        //check guardian  
        //check device  
        return null;
    }

    public async Task<RevokeResultDto> RevokeAsync()
    {
        //get apple userId
        var appleId = "";
        

        await _caHolderAppService.DeleteAsync();
        return new RevokeResultDto()
        {
            Success = true
        };
    }

    private async Task<string> GetCAHashAsync(string chainId, string loginGuardianIdentifierHash)
    {
        var output =
            await _contractProvider.GetHolderInfoAsync(null, Hash.LoadFromHex(loginGuardianIdentifierHash), chainId);

        return output?.CaHash?.ToHex();
    }
}
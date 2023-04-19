using System.Linq;
using System.Threading.Tasks;
using CAServer.Device;
using CAServer.Dtos;
using CAServer.Etos;
using CAServer.Grains;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.Guardian;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
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

    public CAAccountAppService(IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        ILogger<CAAccountAppService> logger, IDeviceAppService deviceAppService)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _logger = logger;
        _deviceAppService = deviceAppService;
    }

    public async Task<AccountResultDto> RegisterRequestAsync(RegisterRequestDto input)
    {
        input.ExtraData = await _deviceAppService.EncryptExtraDataAsync(input.ExtraData);

        var guardianGrainDto = GetGuardian(input.LoginGuardianIdentifier);
        var registerDto = ObjectMapper.Map<RegisterRequestDto, RegisterDto>(input);
        registerDto.GuardianInfo.IdentifierHash = guardianGrainDto.IdentifierHash;

        _logger.LogInformation($"register dto :{JsonConvert.SerializeObject(registerDto)}");

        var grainId = GrainIdHelper.GenerateGrainId(guardianGrainDto.IdentifierHash, input.VerifierId, input.ChainId,
            input.Manager);

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
        input.ExtraData = await _deviceAppService.EncryptExtraDataAsync(input.ExtraData);

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

        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<RecoveryGrainDto, AccountRecoverCreateEto>(result.Data));

        return new AccountResultDto(recoveryDto.Id.ToString());
    }
}
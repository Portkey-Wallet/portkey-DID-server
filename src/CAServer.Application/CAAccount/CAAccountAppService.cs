using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Account;
using CAServer.Dtos;
using CAServer.Etos;
using CAServer.Grains;
using CAServer.Grains.Grain.Account;
using CAServer.Hubs;
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

    public CAAccountAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
    }

    public async Task<AccountResultDto> RegisterRequestAsync(RegisterRequestDto input)
    {
        input.DeviceString = GetDeviceString(input.DeviceString);
        var registerDto = GetRegisterDto(input);

        var grainId = GrainIdHelper.GenerateGrainId(input.LoginGuardianAccount, input.VerifierId, input.ChainId,
            input.ManagerAddress);

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

    public async Task<AccountResultDto> RecoverRequestAsync(RecoveryRequestDto input)
    {
        input.DeviceString = GetDeviceString(input.DeviceString);
        var recoveryDto = GetRecoveryDto(input);
        var grainId = GrainIdHelper.GenerateGrainId(input.LoginGuardianAccount, input.ChainId, input.ManagerAddress);

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

    private string GetDeviceString(string deviceString)
    {
        // var ip = this.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        // string ipStr = ip ?? "";

        return deviceString + "," +
               new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }

    private RegisterDto GetRegisterDto(RegisterRequestDto input)
    {
        var registerDto = new RegisterDto
        {
            Manager = new Manager
            {
                ManagerAddress = input.ManagerAddress,
                DeviceString = input.DeviceString
            },
            GuardianAccountInfo = new GuardianAccountInfo
            {
                Type = (GuardianType)(int)input.Type,
                Value = input.LoginGuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = input.VerifierId,
                    VerificationDoc = input.VerificationDoc,
                    Signature = input.Signature
                }
            },
            ChainId = input.ChainId,
            Id = GuidGenerator.Create(),
            Context = new HubRequestContext
            {
                ClientId = input.Context.ClientId,
                RequestId = input.Context.RequestId,
            }
        };

        return registerDto;
    }

    private RecoveryDto GetRecoveryDto(RecoveryRequestDto input)
    {
        var guardianApproved = new List<GuardianAccountInfo>();

        input.GuardiansApproved?.ForEach(t =>
        {
            guardianApproved.Add(new CAServer.Account.GuardianAccountInfo
            {
                Type = (GuardianType)(int)t.Type,
                Value = t.Value,
                VerificationInfo = new CAServer.Account.VerificationInfo
                {
                    Id = t.VerifierId,
                    VerificationDoc = t.VerificationDoc,
                    Signature = t.Signature
                }
            });
        });

        var recoveryDto = new RecoveryDto
        {
            Manager = new Manager
            {
                ManagerAddress = input.ManagerAddress,
                DeviceString = input.DeviceString
            },
            GuardianApproved = guardianApproved,
            LoginGuardianAccount = input.LoginGuardianAccount,
            ChainId = input.ChainId,
            Id = GuidGenerator.Create(),
            Context = new HubRequestContext
            {
                ClientId = input.Context.ClientId,
                RequestId = input.Context.RequestId,
            }
        };

        return recoveryDto;
    }
}
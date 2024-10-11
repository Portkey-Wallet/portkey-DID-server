using CAServer.Grains.State;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Providers;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Account;

[StorageProvider(ProviderName = "Default")]
public class RecoveryGrain : Grain<RecoveryState>, IRecoveryGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly CAAccountOption _cAAccountOption;

    public RecoveryGrain(IObjectMapper objectMapper, IOptions<CAAccountOption> options)
    {
        _objectMapper = objectMapper;
        _cAAccountOption = options.Value;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, token);
    }

    public async Task<GrainResultDto<RecoveryGrainDto>> RequestAsync(RecoveryGrainDto recoveryGrainDto)
    {
        var result = new GrainResultDto<RecoveryGrainDto>();

        int maxLength = _cAAccountOption.CAAccountRequestInfoMaxLength;
        int expirationTime = _cAAccountOption.CAAccountRequestInfoExpirationTime;

        if (State.RecoveryInfo.Count >= maxLength)
        {
            State.RecoveryInfo.RemoveAll(State.RecoveryInfo.Where(t =>
                t.CreateTime.HasValue && t.CreateTime.Value < DateTime.UtcNow.AddHours(-expirationTime)));
        }

        if (State.RecoveryInfo.Count >= maxLength)
        {
            result.Message = RecoveryMessage.OverCountMessage;
            return result;
        }

        var recoveryInfo = _objectMapper.Map<RecoveryGrainDto, RecoveryInfo>(recoveryGrainDto);
        recoveryInfo.CreateTime = DateTime.UtcNow;

        State.Id = this.GetPrimaryKeyString();
        recoveryInfo.GrainId = State.Id;
        State.RecoveryInfo.Add(recoveryInfo);

        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<RecoveryInfo, RecoveryGrainDto>(recoveryInfo);
        result.Data.Context = recoveryGrainDto.Context;
        result.Data.ReferralInfo = recoveryGrainDto.ReferralInfo;
        return result;
    }

    public async Task<GrainResultDto<RecoveryGrainDto>> UpdateRecoveryResultAsync(
        SocialRecoveryResultGrainDto resultGrainDto)
    {
        var result = new GrainResultDto<RecoveryGrainDto>();

        var recovery = State.RecoveryInfo.FirstOrDefault(t => t.Id == resultGrainDto.Id);
        if (recovery == null)
        {
            result.Message = RecoveryMessage.NotFoundMessage;
            return result;
        }

        recovery.RecoveryTime = resultGrainDto.RecoveryTime;
        recovery.RecoveryMessage = resultGrainDto.RecoveryMessage;
        recovery.RecoverySuccess = resultGrainDto.RecoverySuccess;
        recovery.CaHash = resultGrainDto.CaHash;
        recovery.CaAddress = resultGrainDto.CaAddress;

        await WriteStateAsync();
        result.Success = true;
        result.Data = _objectMapper.Map<RecoveryInfo, RecoveryGrainDto>(recovery);
        return result;
    }
}
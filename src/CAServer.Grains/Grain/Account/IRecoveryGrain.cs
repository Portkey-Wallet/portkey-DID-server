namespace CAServer.Grains.Grain.Account;

public interface IRecoveryGrain : IGrainWithStringKey
{
    Task<GrainResultDto<RecoveryGrainDto>> RequestAsync(RecoveryGrainDto recoveryGrainDto);
    Task<GrainResultDto<RecoveryGrainDto>> UpdateRecoveryResultAsync(SocialRecoveryResultGrainDto resultGrainDto);
}
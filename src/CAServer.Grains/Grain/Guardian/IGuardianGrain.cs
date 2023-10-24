using Orleans;

namespace CAServer.Grains.Grain.Guardian;

public interface IGuardianGrain : IGrainWithStringKey
{
    Task<GrainResultDto<GuardianGrainDto>> AddGuardianAsync(string identifier, string salt, string identifierHash,
        string originalIdentifier = "");

    Task<GrainResultDto<GuardianGrainDto>> GetGuardianAsync(string identifier);
    
    Task<GrainResultDto<GuardianGrainDto>> DeleteGuardian();
}
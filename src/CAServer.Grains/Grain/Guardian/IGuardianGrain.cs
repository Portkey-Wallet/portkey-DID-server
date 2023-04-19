using Orleans;

namespace CAServer.Grains.Grain.Guardian;

public interface IGuardianGrain : IGrainWithStringKey
{
    Task<GrainResultDto<GuardianGrainDto>> AddGuardianAsync(string identifier, string salt, string identifierHash);
    Task<GrainResultDto<GuardianGrainDto>> GetGuardianAsync(string identifier);
}
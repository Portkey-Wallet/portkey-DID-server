using Orleans;

namespace CAServer.Grains.Grain.Guardian;

public interface IGuardianGrain : IGrainWithStringKey
{
    Task<GrainResultDto<GuardianGrainDto>> AddGuardianAsync(string identifier, string salt, string identifierHash,
        string originalIdentifier = "");

    Task<GrainResultDto<GuardianGrainDto>> AddGuardianWithPoseidonHashAsync(string identifier, string salt,
        string identifierHash, string poseidonHash, string originalIdentifier = "");

    Task<GrainResultDto<GuardianGrainDto>> UpdateGuardianAsync(string identifier, string salt,
        string identifierHash);

    Task<GrainResultDto<GuardianGrainDto>> AppendGuardianPoseidonHashAsync(string identifier, string identifierPoseidonHash);

    Task<GrainResultDto<GuardianGrainDto>> GetGuardianAsync(string identifier);
    
    Task<GrainResultDto<GuardianGrainDto>> DeleteGuardian();
}
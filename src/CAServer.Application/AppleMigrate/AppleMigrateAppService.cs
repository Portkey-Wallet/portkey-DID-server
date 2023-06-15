using System.Threading.Tasks;
using CAServer.Grains;
using CAServer.Grains.Grain.Guardian;
using CAServer.Guardian;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.AppleMigrate;

[RemoteService(false)]
[DisableAuditing]
public class AppleMigrateAppService : CAServerAppService, IAppleMigrateAppService
{
    private readonly IClusterClient _clusterClient;

    public AppleMigrateAppService(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<AppleMigrateResponseDto> MigrateAsync(AppleMigrateRequestDto input)
    {
        var guardian = GetGuardian(input.GuardianIdentifier);
        var guardianGrainDto = await AddGuardianAsync(guardian, input.GuardianIdentifier);
        //....
        
        
        return ObjectMapper.Map<GuardianGrainDto, AppleMigrateResponseDto>(guardianGrainDto);
    }

    private GuardianGrainDto GetGuardian(string guardianIdentifier)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);

        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var guardianGrainDto = guardianGrain.GetGuardianAsync(guardianIdentifier).Result;
        if (!guardianGrainDto.Success)
        {
            Logger.LogError($"{guardianGrainDto.Message} guardianIdentifier: {guardianIdentifier}");
            throw new UserFriendlyException(guardianGrainDto.Message, GuardianMessageCode.NotExist);
        }

        return guardianGrainDto.Data;
    }

    private async Task<GuardianGrainDto> AddGuardianAsync(GuardianGrainDto guardianGrainDto, string identifier)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", identifier);
        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var resultDto = await guardianGrain.AddGuardianAsync(identifier, guardianGrainDto.Salt,
            guardianGrainDto.IdentifierHash, guardianGrainDto.Identifier);

        if (!resultDto.Success)
        {
            Logger.LogError($"{resultDto.Message} guardianIdentifier: {identifier}");
            throw new UserFriendlyException(resultDto.Message);
        }

        return resultDto.Data;
    }
}
using CAServer.Dtos;

namespace CAServer.Grains.Grain.Account;

[GenerateSerializer]
public class RecoveryGrainDto : RecoveryDto
{
    [Id(0)]
    public string GrainId { get; set; }
}
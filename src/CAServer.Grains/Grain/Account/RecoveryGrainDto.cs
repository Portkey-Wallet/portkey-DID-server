using CAServer.Dtos;

namespace CAServer.Grains.Grain.Account;

public class RecoveryGrainDto : RecoveryDto
{
    public string GrainId { get; set; }
}
using CAServer.Dtos;

namespace CAServer.Grains.Grain.Account;

public class RegisterGrainDto : RegisterDto
{
    public string GrainId { get; set; }
}
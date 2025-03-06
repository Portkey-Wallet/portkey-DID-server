using CAServer.Dtos;

namespace CAServer.Grains.Grain.Account;

[GenerateSerializer]
public class RegisterGrainDto : RegisterDto
{
    [Id(0)]
    public string GrainId { get; set; }
}
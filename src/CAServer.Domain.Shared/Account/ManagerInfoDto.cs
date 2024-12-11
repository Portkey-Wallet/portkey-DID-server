using Orleans;

namespace CAServer.CAAccount.Dtos.Zklogin;

[GenerateSerializer]
public class ManagerInfoDto
{
    [Id(0)]
    public long Timestamp { get; set; }
    
    [Id(1)]
    public string CaHash { get; set; }
    
    [Id(2)]
    public string ManagerAddress { get; set; }
}
using Orleans;

namespace CAServer.Account;

[GenerateSerializer]
public class ManagerInfo
{
    [Id(0)]
    public string Address { get; set; }
    
    [Id(1)]
    public string ExtraData { get; set; }
}
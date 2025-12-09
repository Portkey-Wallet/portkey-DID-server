using Orleans;

namespace CAServer.CAAccount.Dtos.Zklogin;

[GenerateSerializer]
public class NoncePayload
{
    [Id(0)]
    public ManagerInfoDto AddManager { get; set; }
    
    [Id(1)]
    public ManagerInfoDto RemoveManager { get; set; }
}
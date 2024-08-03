namespace CAServer.CAAccount.Dtos.Zklogin;

public class NoncePayload
{
    public ManagerInfoDto AddManager { get; set; }
    
    public ManagerInfoDto RemoveManager { get; set; }
}
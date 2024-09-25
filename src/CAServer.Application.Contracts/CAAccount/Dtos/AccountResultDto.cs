namespace CAServer.Dtos;

public class AccountResultDto
{
    public AccountResultDto(string sessionId)
    {
        this.SessionId = sessionId;
    }

    public string SessionId { get; set; }
    
    public string CaHash { get; set; }
    
    public string CaAddress { get; set; }
}
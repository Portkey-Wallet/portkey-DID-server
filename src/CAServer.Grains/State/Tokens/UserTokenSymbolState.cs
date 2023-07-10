namespace CAServer.Grains.State.Tokens;

public class UserTokenSymbolState
{
    public Guid UserId { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public bool IsDelete { get; set; }
}
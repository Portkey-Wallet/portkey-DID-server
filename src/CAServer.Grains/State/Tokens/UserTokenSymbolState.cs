namespace CAServer.Grains.State.Tokens;

[GenerateSerializer]
public class UserTokenSymbolState
{
	[Id(0)]
    public Guid UserId { get; set; }
	[Id(1)]
    public string ChainId { get; set; }
	[Id(2)]
    public string Symbol { get; set; }
	[Id(3)]
    public bool IsDelete { get; set; }
}

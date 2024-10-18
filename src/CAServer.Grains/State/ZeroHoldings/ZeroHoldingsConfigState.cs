namespace CAServer.Grains.State.UserExtraInfo;

[GenerateSerializer]
public class ZeroHoldingsConfigState
{
	[Id(0)]
    public string Id { get; set; }
	[Id(1)]
    public Guid UserId { get; set; }
	[Id(2)]
    public string Status { get; set; }
}

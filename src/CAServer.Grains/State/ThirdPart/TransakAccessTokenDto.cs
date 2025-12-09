namespace CAServer.Grains.State.ThirdPart;

[GenerateSerializer]
public class TransakAccessTokenDto
{
	[Id(0)]
    public string Id { get; set; }
	[Id(1)]
    public string AccessToken { get; set; }
	[Id(2)]
    public DateTime ExpireTime { get; set; }
	[Id(3)]
    public DateTime RefreshTime { get; set; }

    // keep last 2 tokens
	[Id(4)]
    public List<TransakAccessTokenDto> History { get; set; } = new ();
}

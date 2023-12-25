namespace CAServer.Grains.State.ThirdPart;

public class TransakAccessTokenDto
{
    public string Id { get; set; }
    public string AccessToken { get; set; }
    public DateTime ExpireTime { get; set; }
    public DateTime RefreshTime { get; set; }

    // keep last 2 tokens
    public List<TransakAccessTokenDto> History { get; set; } = new ();
}
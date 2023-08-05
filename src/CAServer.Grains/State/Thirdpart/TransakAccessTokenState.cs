using CAServer.Grains.Grain.ThirdPart;

namespace CAServer.Grains.State.Thirdpart;

public class TransakAccessTokenState
{
    
    public string Id { get; set; }
    public string AccessToken { get; set; }
    public DateTime ExpireTime { get; set; }
    public DateTime RefreshTime { get; set; }

    // keep last 10 tokens
    public List<TransakAccessTokenDto> History { get; set; } = new ();
}
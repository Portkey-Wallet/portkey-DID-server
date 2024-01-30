using Newtonsoft.Json;

namespace CAServer.TwitterAuth.Dtos;

public class TwitterAuthDto
{
    public string State { get; set; }
    public string Code { get; set; }
    
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }
}
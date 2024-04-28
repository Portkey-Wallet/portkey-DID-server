using Newtonsoft.Json;

namespace CAServer.TwitterAuth.Dtos;

public class TwitterTokenDto
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }
}
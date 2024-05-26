using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace CAServer.Transfer.Dtos;

public class AuthTokenDto
{
    [JsonProperty("access_token")]
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
    [JsonProperty("token_type")]
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }
    [JsonProperty("expires_in")]
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
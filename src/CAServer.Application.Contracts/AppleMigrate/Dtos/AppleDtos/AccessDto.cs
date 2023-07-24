using Newtonsoft.Json;

namespace CAServer.AppleMigrate.Dtos.AppleDtos;

public class AccessDto
{
    [JsonProperty("access_token")] public string AccessToken { get; set; }
}
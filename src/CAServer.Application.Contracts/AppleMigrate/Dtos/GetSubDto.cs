using Newtonsoft.Json;

namespace CAServer.AppleMigrate.Dtos;

public class GetSubDto
{
    public string UserId { get; set; }
    [JsonProperty("transfer_sub")]
    public string Sub { get; set; }
}
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CAServer.Growth.Dtos;

public class TonGiftsRequestDto
{
    
    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("userIds")]
    public List<string> UserIds { get; set; }

    [JsonProperty("taskId")]
    public string TaskId { get; set; }
}
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

    [JsonProperty("k")] // id
    public string K { get; set; }

    [JsonProperty("s")]
    public string S { get; set; } // sign

    [JsonProperty("t")]
    public long T { get; set; } // timestamp
}
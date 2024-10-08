using System.Collections.Generic;

namespace CAServer.Options;

public class TonGiftsOptions
{
    public bool IsStart { get; set; }
    public string ChainId { get; set; }
    public string ToContractAddress { get; set; }
    
    public string HostUrl { get; set; }
    public string Id { get; set; }
    public string ApiKey { get; set; }
    public string TaskId { get; set; }
}
using System.Collections.Generic;

namespace CAServer.Options;

public class IpWhiteListOptions
{
    public Dictionary<string, string> ByPath { get; set; } = new();
    
}
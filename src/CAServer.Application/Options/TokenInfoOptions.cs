using System.Collections.Generic;

namespace CAServer.Options;

public class TokenInfoOptions
{
    public Dictionary<string, TokenInfo> TokenInfos { get; set; }
}

public class TokenInfo
{
    public string ImageUrl { get; set; }
}
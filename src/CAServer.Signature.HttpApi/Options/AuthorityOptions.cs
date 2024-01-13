using System.Collections.Generic;

namespace SignatureServer.Options;

public class AuthorityOptions
{
    
    // appid => appSecret
    public Dictionary<string, string> Dapp { get; set; } = new();
    
    
}
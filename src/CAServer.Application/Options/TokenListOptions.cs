using System.Collections.Generic;

namespace CAServer.Options;

public class TokenListOptions
{
    public List<UserTokenItem> UserToken { get; set; }
    public List<UserTokenItem> SourceToken { get; set; } = new();
}

public class UserTokenItem
{
    public bool IsDefault { get; set; }
    public bool IsDisplay { get; set; }
    public int SortWeight { get; set; }
    public Token Token { get; set; }
    
}

public class Token
{
    public string ChainId {get; set; }
    public string Address { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public string ImageUrl { get; set; }
}
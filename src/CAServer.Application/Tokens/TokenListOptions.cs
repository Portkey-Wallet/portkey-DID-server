using System.Collections.Generic;

namespace CAServer.Tokens;

public class TokenListOptions
{
    public List<UserTokenItem> Token { get; set; }
}

public class UserTokenItem
{
    public string ChainId {get; set; }
    public string Address { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
}
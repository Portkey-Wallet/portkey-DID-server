using System;

namespace CAServer.Tokens;

public class Token
{
    public Guid Id { get; set; }
    public string ChainId {get; set; }
    public string Address { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
}
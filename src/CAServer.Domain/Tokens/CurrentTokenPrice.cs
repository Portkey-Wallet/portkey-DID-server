using System;

namespace CAServer.Tokens;

public class CurrentTokenPrice : TokenPriceBase
{
    public DateTime PriceUpdateTime { get; set; }
}
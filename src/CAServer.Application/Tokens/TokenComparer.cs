using System.Collections.Generic;
using CAServer.Awaken;

namespace CAServer.Tokens;

public class TokenComparer : IEqualityComparer<TradePairsItemToken>
{
    public bool Equals(TradePairsItemToken x, TradePairsItemToken y)
    {
        return y != null && x != null && x.Symbol.Equals(y.Symbol);
    }

    public int GetHashCode(TradePairsItemToken obj)
    {
        return obj.Symbol.GetHashCode();
    }
}
using System;

namespace CAServer.Common;

public static class CalculationHelper
{
    public static decimal GetBalanceInUsd(decimal symbol, long balance, int decimals) =>
        (decimal)((double)(symbol * balance) / Math.Pow(10, decimals));

    
    public static decimal GetBalanceInUsd(decimal? price, int decimals)
    {
        if (!price.HasValue) return 0;

        return (decimal)((double)price.Value / Math.Pow(10, decimals));
    }
}
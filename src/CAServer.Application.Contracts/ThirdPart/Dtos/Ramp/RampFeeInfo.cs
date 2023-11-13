using CAServer.Commons;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampFeeInfo
{
    public FeeItem RampFee { get; set; }
    public FeeItem NetworkFee { get; set; }
}


public class FeeItem 
{
    
    public string Type { get; set; }
    public string Amount { get; set; }
    public string Symbol { get; set; }

    public static FeeItem Fiat(string symbol, string amount)
    {
        return new FeeItem
        {
            Type = CommonConstant.CurrencyFiat,
            Symbol = symbol,
            Amount = amount ?? "0"
        };
    }
    
    public static FeeItem Crypto(string symbol, string amount)
    {
        return new FeeItem
        {
            Type = CommonConstant.CurrencyCrypto,
            Symbol = symbol,
            Amount = amount ?? "0"
        };
    }
    
}
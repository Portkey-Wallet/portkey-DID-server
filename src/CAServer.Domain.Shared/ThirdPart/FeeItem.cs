namespace CAServer.ThirdPart;

public class FeeItem 
{
    
    public string Type { get; set; }
    public string Amount { get; set; }
    public string Symbol { get; set; }
    public string SymbolPriceInUsdt { get; set; }

    public static FeeItem Fiat(string symbol, string amount)
    {
        return new FeeItem
        {
            Type = "Fiat",
            Symbol = symbol,
            Amount = amount ?? "0"
        };
    }
    
    public static FeeItem Crypto(string symbol, string amount, decimal? symbolPriceInUsdt = null)
    {
        return new FeeItem
        {
            Type = "Crypto",
            Symbol = symbol,
            Amount = amount ?? "0",
            SymbolPriceInUsdt = symbolPriceInUsdt.ToString()
        };
    }
    
}

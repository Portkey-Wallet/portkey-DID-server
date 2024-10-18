using Orleans;

namespace CAServer.ThirdPart;

[GenerateSerializer]
public class FeeItem 
{
    [Id(0)]
    public string Type { get; set; }
    [Id(1)]
    public string Amount { get; set; }
    [Id(2)]
    public string Symbol { get; set; }
    [Id(3)]
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

using System.ComponentModel.DataAnnotations;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampLimitDto
{
    public CurrencyLimit Crypto { get; set; }
    public CurrencyLimit Fiat { get; set; }
}

public class CurrencyLimit
{
    public string Symbol { get; set; }
    public string MinLimit { get; set; }
    public string MaxLimit { get; set; }

    public CurrencyLimit()
    {
    }

    public CurrencyLimit(string symbol, string minLimit, string maxLimit)
    {
        Symbol = symbol;
        MinLimit = minLimit;
        MaxLimit = maxLimit;
    }
}


public class RampLimitRequest
{
    [Required] public string Type { get; set; }
    [Required] public string Crypto { get; set; }
    public string Network { get; set; }
    [Required] public string Fiat { get; set; }
    [Required] public string Country { get; set; }
    
    public bool IsBuy()
    {
        return Type == OrderTransDirect.BUY.ToString();
    }
    
    public bool IsSell()
    {
        return Type == OrderTransDirect.SELL.ToString();
    }
}
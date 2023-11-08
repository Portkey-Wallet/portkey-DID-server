namespace CAServer.ThirdPart.Dtos.Ramp;

public class DefaultFiatCurrency
{
    public string Symbol { get; set; }
    public string Amount { get; set; }
    public string Country { get; set; }
    public string CountryName { get; set; }
}

public class DefaultCryptoCurrency
{
    public string Symbol { get; set; }
    public string Amount { get; set; }
    public string Network { get; set; }
}
using CAServer.Commons.Etos;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class DefaultFiatCurrency
{
    public string Symbol { get; set; }
    public string Amount { get; set; }
    public string Country { get; set; }
    public string CountryName { get; set; }
    public string Icon { get; set; }
}

public class DefaultCryptoCurrency : ChainDisplayNameDto
{
    public string Symbol { get; set; }
    public string Amount { get; set; }
    public string Network { get; set; }
    public string Icon { get; set; }

}
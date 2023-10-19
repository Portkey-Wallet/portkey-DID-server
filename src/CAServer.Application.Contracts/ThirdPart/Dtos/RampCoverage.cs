using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos;

public class RampCoverage
{
    public bool Sell { get; set; } = true;
    public List<CryptoItem> CryptoList { get; set; }
    public List<FiatItem> FiatList { get; set; }
}

public class CryptoItem
{
    public string Symbol { get; set; }
    public string Icon { get; set; }
}

public class FiatItem
{
    public string Symbol { get; set; }
    public string Icon { get; set; }
    public string Country { get; set; }
    public string CountryName { get; set; }
    public string MinLimit { get; set; }
    public string MaxLimit { get; set; }
}
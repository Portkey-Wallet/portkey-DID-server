using System.Collections.Generic;
using CAServer.Commons;

namespace CAServer.ThirdPart.Dtos;

public class RampFiatDto
{
    public List<RampFiatItem> FiatList { get; set; }
    public string DefaultFiat { get; set; } = CommonConstant.DefaultFiatUSD;
}


public class RampFiatItem
{
    public string Country { get; set;}
    public string Symbol { get; set;}
    public string CountryName { get; set;}
    public string Icon { get; set;}
    public string BuyMinLimit { get; set;}
    public string BuyMaxLimit { get; set;}
    public string SellMinLimit { get; set;}
    public string SellMaxLimit { get; set;}
}
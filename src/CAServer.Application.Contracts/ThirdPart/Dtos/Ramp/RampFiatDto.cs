using System.Collections.Generic;
using CAServer.Commons;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampFiatDto
{
    public List<RampFiatItem> FiatList { get; set; }
    public DefaultCurrency DefaultFiat { get; set; }
}


public class RampFiatItem
{
    public string Country { get; set;}
    public string Symbol { get; set;}
    public string CountryName { get; set;}
    public string Icon { get; set;}
}
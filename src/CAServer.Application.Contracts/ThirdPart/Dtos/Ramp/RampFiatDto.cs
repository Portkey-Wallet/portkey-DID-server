using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CAServer.Commons;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampFiatRequest
{
    [Required] public string Type { get; set; }
    public string Crypto { get; set; }
    public string Network { get; set; }
}


public class RampFiatDto
{
    public List<RampFiatItem> FiatList { get; set; }
    public DefaultFiatCurrency DefaultFiat { get; set; }
}


public class RampFiatItem
{
    public string Country { get; set;}
    public string Symbol { get; set;}
    public string CountryName { get; set;}
    public string Icon { get; set;}
}
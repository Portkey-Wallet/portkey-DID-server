using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampExchangeDto
{
    public string Exchange { get; set; }
}

public class RampExchangeRequest
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
    
    
    public override string ToString()
    {
        return $"RampExchangeRequest(type={Type}, Crypto={Crypto}，Network={Network},Fiat={Fiat},Country:{Country})";
    }
}
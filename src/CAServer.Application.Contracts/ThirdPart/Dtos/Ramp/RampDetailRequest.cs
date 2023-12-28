using System.ComponentModel.DataAnnotations;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampDetailRequest
{
    /// <see cref="OrderTransDirect"/>
    [Required] public string Type { get; set; }
    [Required] public string Crypto { get; set; }
    public decimal? CryptoAmount { get; set; }
    
    [Required] public string Fiat { get; set; }
    public decimal? FiatAmount { set; get; }
    
    [Required] public string Country { get; set; }
    public string Network { get; set; }


    public bool IsBuy()
    {
        return Type == OrderTransDirect.BUY.ToString();
    }
    
    public bool IsSell()
    {
        return Type == OrderTransDirect.SELL.ToString();
    }
}
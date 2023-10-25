using System.ComponentModel.DataAnnotations;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampDetailRequest
{
    /// <see cref="OrderTransDirect"/>
    [Required] public string Type { get; set; }
    [Required] public string Crypto { get; set; }
    [Required] public string Fiat { get; set; }
    [Required] public string Country { get; set; }
    [Required] public decimal Price { get; set; }
    public string Network { get; set; }
    
}
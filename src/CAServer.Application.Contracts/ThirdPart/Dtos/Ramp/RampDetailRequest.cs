namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampDetailRequest
{
    /// <see cref="OrderTransDirect"/>
    public string Type { get; set; }
    
    public string Crypto { get; set; }
    public string Fiat { get; set; }
    public string Country { get; set; }
    public decimal Price { get; set; }
    public string Network { get; set; }
    
}
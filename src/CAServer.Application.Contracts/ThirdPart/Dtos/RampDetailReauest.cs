namespace CAServer.ThirdPart.Dtos;

public class RampDetailRequest
{
    /// <see cref="OrderTransDirect"/>
    public string Type { get; set; }
    
    /// <summary>
    ///     On-ramp: Crypto => Fiat
    ///     Off-ramp: Fiat => crypto 
    /// </summary>
    public string Crypto { get; set; }
    public string Fiat { get; set; }
    
    /// <summary>
    ///     price with decimal
    ///     1.23 ELF: price = 1.23
    ///     1.23 USD: price = 1.23
    /// </summary>
    public decimal Price { get; set; }
    
}
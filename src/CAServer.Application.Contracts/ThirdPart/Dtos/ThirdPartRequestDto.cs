using System.ComponentModel.DataAnnotations;

namespace CAServer.ThirdPart.Dtos;

public class QueryFiatRequestDto
{
    [Required] public string Type { get; set; } = OrderTransDirect.BUY.ToString();
    public string Crypto { get; set; }
}

public class QueryCurrencyRequestDto
{
    [Required] public string Type { get; set; } = OrderTransDirect.BUY.ToString();
    public string Fiat { get; set; }
    public string Crypto { get; set; }
}

public class QueryPriceRequestDto
{
    [Required] public string Crypto { get; set; }
    [Required] public string Network { get; set; }
    [Required] public string Fiat { get; set; }
    [Required] public string Country { get; set; }
    [Required] public string PaymentCode { get; set; }
    // BUY or SELL
    [Required] public string Type { get; set; } = OrderTransDirect.BUY.ToString();
    // for SELL
    [Required] public string CryptoAmount { get; set; }
    // for BUY
    [Required] public string FiatAmount { get; set; }
}
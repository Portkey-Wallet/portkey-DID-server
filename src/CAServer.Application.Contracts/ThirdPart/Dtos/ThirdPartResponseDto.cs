using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos;

public class QueryPriceResponseDto
{
    public string CryptoAmount { get; set; }
    public string CryptoSymbol { get; set; }
    public string FiatAmount { get; set; }
    public string Fiat { get; set; }
    public string RampFee { get; set; }
    public string NetworkFee { get; set; }
    public string TotalFee { get; set; }
    public string PaymentCode { get; set; }
    
    public string LimitCurrency { get; set; }
    public string MaxAmount { get; set; }
    public string MinAmount { get; set; }
}

public class QueryCryptoResponseDto
{
    public List<CryptoItem> CryptoList { get; set; } = new List<CryptoItem>();
}

public class CryptoItem
{
    public string CryptoSymbol { get; set; }
    public string Network { get; set; }
    public string Icon { get; set; }
    public List<FiatItem> FiatList { get; set; } = new List<FiatItem>();
}

public class QueryFiatResponseDto
{
    public List<FiatItem> FiatList { get; set; } = new List<FiatItem>();
}

public class FiatItem
{
    public string Fiat { get; set; }
    public string Country { get; set; }
    public List<FiatPayment> FiatPayments { get; set; } = new();
}

public class FiatPayment
{
    public string PaymentCode { get; set; }
    public string LimitCurrency { get; set; }
    public string MinAmount { get; set; }
    public string MaxAmount { get; set; }
}
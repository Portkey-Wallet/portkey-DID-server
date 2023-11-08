using System.Collections.Generic;
using CAServer.Commons;

namespace CAServer.ThirdPart.Dtos.Ramp;


public class RampCryptoRequest
{
    public string Type { get; set; }
    public string Fiat { get; set; }
    public string Country { get; set; }
}

public class RampCryptoDto
{
    public List<RampCurrencyItem> CryptoList { get; set; } = new List<RampCurrencyItem>();
    public DefaultCryptoCurrency DefaultCrypto { get; set; }
}


public class RampCurrencyItem
{
    public string Symbol { get; set; }
    public string Icon { get; set; }
    public string Decimals { get; set; }
    public string Network { get; set; }
    public string Address { get; set; }
}

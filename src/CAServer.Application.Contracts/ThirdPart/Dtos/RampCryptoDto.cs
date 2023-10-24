using System.Collections.Generic;
using CAServer.Commons;

namespace CAServer.ThirdPart.Dtos;

public class RampCryptoDto
{
    public List<RampCurrencyItem> CryptoList { get; set; }
    public string DefaultCrypto { get; set; } = CommonConstant.DefaultCryptoELF;
}


public class RampCurrencyItem
{
    public string Symbol { get; set; }
    public string Icon { get; set; }
    public string Decimals { get; set; }
    public string BuyMinLimit { get; set; }
    public string BuyMaxLimit { get; set; }
    public string SellMinLimit { get; set; }
    public string SellMaxLimit { get; set; }
    public string Network { get; set; }
    public string Address { get; set; }
}

using System;
using System.Collections.Generic;

namespace CAServer.ThirdPart;

public class OrderSettlementGrainDto
{
    public Guid Id { get; set; }
    public string TotalFee { get; set; }
    public string FeeCurrency { get; set; }
    public decimal? BinanceExchange { get; set; }
    public decimal? OkxExchange { get; set; }
    public string SettlementCurrency { get; set; }
    public decimal? BinanceSettlementAmount { get; set; }
    public decimal? OkxSettlementAmount { get; set; }
    public List<FeeItem> FeeDetail { get; set; } = new();
    public Dictionary<string, string> ExtensionData { get; set; }
}

public class FeeItem
{
    public string Name { get; set; }
    public string FiatCrypto { get; set; }
    public string Currency { get; set; }
    public string FeeAmount { get; set; }
}
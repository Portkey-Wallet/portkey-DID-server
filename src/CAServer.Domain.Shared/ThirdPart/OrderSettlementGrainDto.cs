using System;
using System.Collections.Generic;
using Orleans;

namespace CAServer.ThirdPart;

[GenerateSerializer]
public class OrderSettlementGrainDto
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string TotalFee { get; set; }
    [Id(2)]
    public string FeeCurrency { get; set; }
    [Id(3)]
    public decimal? BinanceExchange { get; set; }
    [Id(4)]
    public decimal? OkxExchange { get; set; }
    [Id(5)]
    public string SettlementCurrency { get; set; }
    [Id(6)]
    public decimal? BinanceSettlementAmount { get; set; }
    [Id(7)]
    public decimal? OkxSettlementAmount { get; set; }
    [Id(8)]
    public List<FeeItem> FeeDetail { get; set; } = new();
    [Id(9)]
    public Dictionary<string, string> ExtensionData { get; set; }
}
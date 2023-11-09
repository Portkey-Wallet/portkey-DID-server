using System;
using System.Collections.Generic;

namespace CAServer.ThirdPart;

public class OrderSettlementGrainDto
{
    public Guid Id { get; set; }
    public string TotalFee { get; set; }
    public string FeeCurrency { get; set; }
    public decimal? ExchangeFiatUsd { get; set; }
    public decimal? ExchangeUsdUsdt { get; set; }
    public decimal? SettlementUsdt { get; set; }
    public List<FeeItem> FeeDetail { get; set; }
}

public class FeeItem
{
    public string Name { get; set; }
    public string FiatCrypto { get; set; }
    public string Currency { get; set; }
    public string FeeAmount { get; set; }
}
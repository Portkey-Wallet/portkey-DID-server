using System;
using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos.Order;

public class OrderSettlementSectionDto : BaseOrderSection
{

    public OrderSettlementSectionDto() : base(OrderSectionEnum.SettlementSection)
    {
    }

    public Guid Id { get; set; }
    public string TotalFee { get; set; }
    public string FeeCurrency { get; set; }
    public string ExchangeFiatUsd { get; set; }
    public string ExchangeUsdUsdt { get; set; }
    public string SettlementUsdt { get; set; }
    public string ExchangeUsdCrypto { get; set; }
    public string SettlementCurrency { get; set; }
    public string SettlementAmount { get; set; }
    
    public List<FeeItem> FeeDetail { get; set; }
    
}


public class FeeItem
{
    public string Name { get; set; }
    public string FiatCrypto { get; set; }
    public string Currency { get; set; }
    public string FeeAmount { get; set; }
}
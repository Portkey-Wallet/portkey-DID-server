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
    public string BinanceExchange { get; set; }
    public string OkxExchange { get; set; }
    public string SettlementCurrency { get; set; }
    public string BinanceSettlementAmount { get; set; }
    public string OkxSettlementAmount { get; set; }
    public List<FeeItem> FeeDetail { get; set; }
    
}
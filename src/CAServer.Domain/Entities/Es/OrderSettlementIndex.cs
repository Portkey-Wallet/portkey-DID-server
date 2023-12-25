using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using CAServer.ThirdPart;
using Nest;

namespace CAServer.Entities.Es;

public class OrderSettlementIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public string TotalFee { get; set; }
    [Keyword] public string FeeCurrency { get; set; }
    public decimal? BinanceExchange { get; set; }
    public decimal? OkxExchange { get; set; }
    [Keyword] public string SettlementCurrency { get; set; }
    public decimal? BinanceSettlementAmount { get; set; }
    public decimal? OkxSettlementAmount { get; set; }
    public List<FeeItem> FeeDetail { get; set; }
    public Dictionary<string, string> ExtensionData { get; set; }
}
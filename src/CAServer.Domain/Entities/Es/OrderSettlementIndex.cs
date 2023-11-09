using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class OrderSettlementIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public string TotalFee { get; set; }
    [Keyword] public string FeeCurrency { get; set; }
    [Keyword] public decimal? ExchangeFiatUsd { get; set; }
    [Keyword] public decimal? ExchangeUsdUsdt { get; set; }
    [Keyword] public decimal? SettlementUsdt { get; set; }
    public List<FeeItem> FeeDetail { get; set; }
}

public class FeeItem
{
    public string Name { get; set; }
    public string FiatCrypto { get; set; }
    public string Currency { get; set; }
    public string FeeAmount { get; set; }
}
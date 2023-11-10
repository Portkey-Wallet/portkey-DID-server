using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class OrderSettlementIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public string TotalFee { get; set; }
    [Keyword] public string FeeCurrency { get; set; }
    public decimal? ExchangeFiatUsd { get; set; }
    public decimal? ExchangeUsdUsdt { get; set; }
    public decimal? ExchangeUsdCrypto { get; set; }
    public string SettlementCurrency { get; set; }
    public decimal? SettlementAmount { get; set; }
    public List<FeeItem> FeeDetail { get; set; }
}

public class FeeItem
{
    public string Name { get; set; }
    public string FiatCrypto { get; set; }
    public string Currency { get; set; }
    public string FeeAmount { get; set; }
}
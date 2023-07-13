using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class AlchemyOrderIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public string OrderNo { get; set; }
    public string Status { get; set; }
    public string Side { get; set; }
    public string Address { get; set; } = "";
    public string PayTime { get; set; } = "";
    public string CompleteTime { get; set; } = "";
    public string MerchantOrderNo { get; set; } = "";
    public string Crypto { get; set; } = "";
    public string Network { get; set; } = "";
    public string CryptoPrice { get; set; } = "";
    public string CryptoAmount { get; set; } = "";
    public string FiatAmount { get; set; } = "";
    public string AppId { get; set; } = "";
    public string Fiat { get; set; } = "";
    public string TxHash { get; set; } = "";
    public string Email { get; set; } = "";
    public string OrderAddress { get; set; } = "";
    public string RampFee { get; set; } = "";
    public string PaymentType { get; set; } = "";
    public string Name { get; set; } = "";
    public string Account { get; set; } = "";
    public string FiatRate { get; set; } = "";
    public string Amount { get; set; } = "";
    public string TxTime { get; set; } = "";
    public string Networkfee { get; set; } = "";
    public string PayType { get; set; } = "";
    public string CryptoQuantity { get; set; } = "";
    public string CryptoActualAmount { get; set; } = "";
}
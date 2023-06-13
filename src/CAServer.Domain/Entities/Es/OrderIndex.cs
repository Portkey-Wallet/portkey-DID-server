using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using Volo.Abp.Domain.Entities;

namespace CAServer.Entities.Es;

public class OrderIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public Guid UserId { get; set; }
    public object ThirdPartOrderNo { get; set; }
    public string TransDirect { get; set; }
    public string MerchantName { get; set; }
    public string Address { get; set; }
    public string Crypto { get; set; }
    public string CryptoPrice { get; set; }
    public string Network { get; set; }
    public string CryptoAmount { get; set; }
    public string Fiat { get; set; }
    public string FiatAmount { get; set; }
    public string LastModifyTime { get; set; }
    public bool IsDeleted { get; set; } = false;
    public string Status { get; set; }

    // buy order
    public string CryptoQuantity { get; set; }
    public string PaymentMethod { get; set; }
    public string TxTime { get; set; }

    // sell order
    public string ReceivingMethod { get; set; }
    public string ReceiptTime { get; set; }
}
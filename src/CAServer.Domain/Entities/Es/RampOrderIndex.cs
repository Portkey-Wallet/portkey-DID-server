using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class RampOrderIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public Guid UserId { get; set; }
    [Keyword] public string ThirdPartOrderNo { get; set; }
    [Keyword] public string TransactionHash { get; set; }
    [Keyword] public string TransDirect { get; set; }
    [Keyword] public string MerchantName { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string Crypto { get; set; }
    [Keyword] public string CryptoPrice { get; set; }
    [Keyword] public string Network { get; set; }
    [Keyword] public string CryptoAmount { get; set; }
    [Keyword] public string Fiat { get; set; }
    [Keyword] public string FiatAmount { get; set; }
    [Keyword] public string LastModifyTime { get; set; }
    [Keyword] public bool IsDeleted { get; set; } = false;
    [Keyword] public string Status { get; set; }

    // buy order
    [Keyword] public string CryptoQuantity { get; set; }
    [Keyword] public string PaymentMethod { get; set; }
    [Keyword] public string TxTime { get; set; }

    // sell order
    [Keyword] public string ReceivingMethod { get; set; }
    [Keyword] public string ReceiptTime { get; set; }
}
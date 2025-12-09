using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using CAServer.ThirdPart;
using CAServer.Tokens;
using Nest;

namespace CAServer.Entities.Es;

public class TreasuryOrderIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    public Guid RampOrderId { get; set; }
    public int Version { get; set; }
    [Keyword] public string ThirdPartName { get; set; }
    
    /// <see cref="TransferDirectionType"/>
    [Keyword] public string TransferDirection { get; set; }
    [Keyword] public string ThirdPartOrderId { get; set; }
    [Keyword] public string Network { get; set; }
    [Keyword] public string ThirdPartNetwork { get; set; }
    [Keyword] public string ToAddress { get; set; }
    
    /// <see cref="OrderStatusType"/>
    [Keyword] public string Status { get; set; }
    [Keyword] public string Crypto { get; set; }
    [Keyword] public string ThirdPartCrypto { get; set; }
    public decimal CryptoPriceInUsdt { get; set; }
    public decimal CryptoAmount { get; set; }
    public int CryptoDecimals { get; set; }
    [Keyword] public string Fiat { get; set; }
    public decimal FiatAmount { get; set; }
    public decimal SettlementAmount { get; set; }
    public List<FeeItem> FeeInfo { get; set; } = new();
    public List<TokenExchange> TokenExchanges { get; set; } = new(); 
    
    [Keyword] public string TransactionId { get; set; }
    public string RawTransaction { get; set; }
    public int TxRetryTimes { get; set; }
    public long TransactionTime { get; set; }
    
    /// <see cref="TreasuryCallBackStatus"/>
    [Keyword] public string CallbackStatus { get; set; }
    public string CallBackResult { get; set; }
    public int CallbackCount { get; set; }
    public long CallbackTime { get; set; }
    
    public long CreateTime { get; set; }
    public long LastModifyTime { get; set; }
    
    
}
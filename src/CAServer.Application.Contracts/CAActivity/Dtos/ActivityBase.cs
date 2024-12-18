using System.Collections.Generic;

namespace CAServer.CAActivity.Dtos;

public class ActivityBase
{
    public string TransactionId { get; set; }
    public string BlockHash { get; set; }
    public string TransactionType { get; set; }
    public string TransactionName { get; set; }
    public string Amount { get; set; }
    public string Symbol { get; set; }
    public string Decimals { get; set; }
    public string Status { get; set; }
    public string Timestamp { get; set; }
    public bool IsReceived { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public string FromChainId { get; set; }
    public string FromChainIdUpdated { get; set; }
    public string FromChainIcon { get; set; }
    public string ToChainId { get; set; }
    public string ToChainIdUpdated { get; set; }
    public string ToChainIcon { get; set; }
    public List<TransactionFee> TransactionFees { get; set; }
    public string PriceInUsd { get; set; }
    public bool IsDelegated { get; set; }
    public bool IsSystem { get; set; }
    public string StatusIcon { get; set; }
    
    public string SourceIcon { get; set; }
}

public class TransactionFee
{
    public string Symbol { get; set; }
    public long? Fee { get; set; }
    public string FeeInUsd { get; set; }
    public string Decimals { get; set; }
}
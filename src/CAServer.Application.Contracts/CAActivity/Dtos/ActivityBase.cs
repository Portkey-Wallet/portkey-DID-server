using System.Collections.Generic;
using Orleans;

namespace CAServer.CAActivity.Dtos;

[GenerateSerializer]
public class ActivityBase
{
    [Id(0)]
    public string TransactionId { get; set; }
    [Id(1)]
    public string BlockHash { get; set; }
    [Id(2)]
    public string TransactionType { get; set; }
    [Id(3)]
    public string TransactionName { get; set; }
    [Id(4)]
    public string Amount { get; set; }
    [Id(5)]
    public string Symbol { get; set; }
    [Id(6)]
    public string Decimals { get; set; }
    [Id(7)]
    public string Status { get; set; }
    [Id(8)]
    public string Timestamp { get; set; }
    [Id(9)]
    public bool IsReceived { get; set; }
    [Id(10)]
    public string From { get; set; }
    [Id(11)]
    public string To { get; set; }
    [Id(12)]
    public string FromAddress { get; set; }
    [Id(13)]
    public string ToAddress { get; set; }
    [Id(14)]
    public string FromChainId { get; set; }
    [Id(15)]
    public string FromChainIdUpdated { get; set; }
    [Id(16)]
    public string FromChainIcon { get; set; }
    [Id(17)]
    public string ToChainId { get; set; }
    [Id(18)]
    public string ToChainIdUpdated { get; set; }
    [Id(19)]
    public string ToChainIcon { get; set; }
    [Id(20)]
    public List<TransactionFee> TransactionFees { get; set; }
    [Id(21)]
    public string PriceInUsd { get; set; }
    [Id(22)]
    public bool IsDelegated { get; set; }
    [Id(23)]
    public bool IsSystem { get; set; }
    [Id(24)]
    public string StatusIcon { get; set; }
    [Id(25)]
    public string SourceIcon { get; set; }
}

[GenerateSerializer]
public class TransactionFee
{
    [Id(0)]
    public string Symbol { get; set; }
    [Id(1)]
    public long? Fee { get; set; }
    [Id(2)]
    public string FeeInUsd { get; set; }
    [Id(3)]
    public string Decimals { get; set; }
}
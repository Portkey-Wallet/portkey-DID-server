using System;
using System.Collections.Generic;
using CAServer.Tokens;
using Orleans;

namespace CAServer.ThirdPart;

[GenerateSerializer]
public class TreasuryOrderDto
{

    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public Guid RampOrderId { get; set; }
    [Id(2)]
    public string ThirdPartName { get; set; }
    [Id(3)]
    public int Version { get; set; }

    /// <see cref="TransferDirectionType"/>
    [Id(4)]
    public string TransferDirection { get; set; }
    [Id(5)]
    public string ThirdPartOrderId { get; set; }
    [Id(6)]
    public string Network { get; set; }
    [Id(7)]
    public string ThirdPartNetwork { get; set; }
    [Id(8)]
    public string ToAddress { get; set; }

    /// <see cref="OrderStatusType"/>
    [Id(9)]
    public string Status { get; set; }

    [Id(10)]
    public string Crypto { get; set; }
    [Id(11)]
    public string ThirdPartCrypto { get; set; }
    [Id(12)]
    public decimal CryptoPriceInUsdt { get; set; }
    [Id(13)]
    public decimal CryptoAmount { get; set; }
    [Id(14)]
    public int CryptoDecimals { get; set; }
    [Id(15)]
    public string Fiat { get; set; }
    [Id(16)]
    public decimal FiatAmount { get; set; }
    [Id(17)]
    public string SettlementAmount { get; set; }
    [Id(18)]
    public List<FeeItem> FeeInfo { get; set; } = new();
    [Id(19)]
    public List<TokenExchange> TokenExchanges { get; set; } = new();

    [Id(20)]
    public string TransactionId { get; set; }
    [Id(21)]
    public string RawTransaction { get; set; }
    [Id(22)]
    public int TxRetryTimes { get; set; }
    [Id(23)]
    public long TransactionTime { get; set; }

    /// <see cref="TreasuryCallBackStatus"/>
    [Id(24)]
    public string CallbackStatus { get; set; }
    [Id(25)]
    public string CallBackResult { get; set; }
    [Id(26)]
    public int CallbackCount { get; set; }
    [Id(27)]
    public long CallbackTime { get; set; }

    [Id(28)]
    public long CreateTime { get; set; }
    [Id(29)]
    public long LastModifyTime { get; set; }

}
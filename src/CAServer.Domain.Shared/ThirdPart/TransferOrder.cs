using System;
using System.Collections.Generic;

namespace CAServer.ThirdPart;

public class TransferOrder
{
    
    public Guid Id { get; set; }
    public Guid RampOrderId { get; set; }
    public string ThirdPartName { get; set; }
    public string ThirdPartOrderId { get; set; }
    public string Network { get; set; }
    public string ToAddress { get; set; }
    public string Status { get; set; }
    
    public string Crypto { get; set; }
    public decimal CryptoPriceInUsdt { get; set; }
    public string CryptoAmount { get; set; }
    public int CryptoDecimals { get; set; }
    public string Fiat { get; set; }
    public decimal FiatAmount { get; set; }
    public List<FeeItem> FeeInfo { get; set; }
    
    public string TransactionId { get; set; }
    public string RawTransaction { get; set; }
    public int TxRetryTimes { get; set; }
    public long TransactionTime { get; set; }
    
    public string CallbackCount { get; set; }
    public string CallbackStatus { get; set; }
    public long CallbackTime { get; set; }
    
    public long CreateTime { get; set; }
    public long LastModifyTime { get; set; }
    
}
using System;
using System.Collections.Generic;

namespace CAServer.ThirdPart;

public class TreasuryOrderDto
{
    
    public Guid Id { get; set; }
    public Guid RampOrderId { get; set; }
    public string ThirdPartName { get; set; }
    
    /// <see cref="TransferDirectionType"/>
    public string TransferDirection { get; set; }
    public string ThirdPartOrderId { get; set; }
    public string Network { get; set; }
    public string ToAddress { get; set; }
    
    /// <see cref="OrderStatusType"/>
    public string Status { get; set; }
    
    public string Crypto { get; set; }
    public decimal CryptoPriceInUsdt { get; set; }
    public string CryptoAmount { get; set; }
    public int CryptoDecimals { get; set; }
    public string Fiat { get; set; }
    public decimal FiatAmount { get; set; }
    public string SettlementAmount { get; set; }
    public List<FeeItem> FeeInfo { get; set; }
    
    public string TransactionId { get; set; }
    public string RawTransaction { get; set; }
    public int TxRetryTimes { get; set; }
    public long TransactionTime { get; set; }
    
    /// <see cref="TreasuryCallBackStatus"/>
    public string CallbackStatus { get; set; }
    public string CallBackResult { get; set; }
    public int CallbackCount { get; set; }
    public long CallbackTime { get; set; }
    
    public long CreateTime { get; set; }
    public long LastModifyTime { get; set; }
    
}
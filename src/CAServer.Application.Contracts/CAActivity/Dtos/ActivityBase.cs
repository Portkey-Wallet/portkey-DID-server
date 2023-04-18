using System.Collections.Generic;

namespace CAServer.CAActivity.Dto;

public class ActivityBase
{
    public string TransactionId { get; set; }
    public string BlockHash { get; set; }
    public string TransactionType { get; set; }
    public string Amount { get; set; }
    public string Symbol { get; set; }
    public string Decimal { get; set; }

    public string Status { get; set; }
    public string TimeStamp { get; set; }

    public string From { get; set; }
    public string To { get; set; }
    public string FromAddress { get; set; }

    public string ToAddress { get; set; }

    public string FromChainId { get; set; }
    public string ToChainId { get; set; }

    public List<TransactionFee> TransactionFees { get; set; }

    public string PriceInUsd { get; set; }
}

public class TransactionFee
{
    public string Symbol { get; set; }
    public long? Fee { get; set; }
    public string FeeInUsd { get; set; }
}
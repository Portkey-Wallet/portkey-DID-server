using System.Collections.Generic;

namespace CAServer.Options;

public class ActivityOptions
{
    public List<TransactionFeeFixHeight> ActivityTransactionFeeFixHeight { get; set; }
}

public class TransactionFeeFixHeight
{
    public string ChainId { get; set; }
    public long StartBlock { get; set; }
}


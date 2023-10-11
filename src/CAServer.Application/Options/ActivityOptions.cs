using System.Collections.Generic;

namespace CAServer.Options;

public class ActivityOptions
{
    public List<TransactionFeeFix> ActivityTransactionFeeFix { get; set; }
}

public class TransactionFeeFix
{
    public string ChainId { get; set; }
    public long StartBlock { get; set; }
}


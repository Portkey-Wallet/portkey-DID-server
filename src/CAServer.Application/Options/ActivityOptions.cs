using System.Collections.Generic;

namespace CAServer.Options;

public class ActivityOptions
{
    public List<TransactionFeeFix> ActivityTransactionFeeFix { get; set; }
    public List<ETransConfig> ETransConfigs { get; set; }
}

public class ETransConfig
{
    public string ChainId { get; set; }
    public List<string> Accounts { get; set; }
}

public class TransactionFeeFix
{
    public string ChainId { get; set; }
    public long StartBlock { get; set; }
}
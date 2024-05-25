using System.Collections.Generic;

namespace CAServer.Options;

public class ActivityOptions
{
    public List<TransactionFeeFix> ActivityTransactionFeeFix { get; set; }
    public List<ETransferConfig> ETransferConfigs { get; set; }
}

public class ETransferConfig
{
    public string ChainId { get; set; }
    public List<string> Accounts { get; set; }
    public string ContractAddress { get; set; }
}

public class TransactionFeeFix
{
    public string ChainId { get; set; }
    public long StartBlock { get; set; }
}
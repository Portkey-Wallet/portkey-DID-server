using System.Collections.Generic;

namespace CAServer.Options;

public class ActivityOptions
{
    public List<TransactionFeeFix> ActivityTransactionFeeFix { get; set; }
    public List<ETransferConfig> ETransferConfigs { get; set; }
    public List<ContractConfig> ContractConfigs { get; set; }
    public HamsterConfig HamsterConfig { get; set; }
    public UnknownConfig UnknownConfig { get; set; }
}

public class ETransferConfig
{
    public string ChainId { get; set; }
    public List<string> Accounts { get; set; }
    public string ContractAddress { get; set; }
}

public class HamsterConfig
{
    public string ContractAddress { get; set; }
    public string GetPassName { get; set; }
    public string GetRewardName { get; set; }
    public string FromAddress { get; set; }
}

public class TransactionFeeFix
{
    public string ChainId { get; set; }
    public long StartBlock { get; set; }
}

public class ContractConfig
{
    public string ContractAddress { get; set; }
    // display name and icon, maybe not dapp, just contract.
    public string DappName { get; set; }
    public string DappIcon { get; set; }
    public Dictionary<string, string> MethodNameMap { get; set; } = new();
}

public class UnknownConfig
{
    public string UnknownIcon { get; set; }
    public string UnknownName { get; set; }
    public List<string> NotUnknownContracts { get; set; } = new();
}
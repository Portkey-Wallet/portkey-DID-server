using System.Collections.Generic;

namespace CAServer.CAActivity;

public static class ActivityConstants
{
    public static readonly List<string> DefaultTypes = new()
    {
        "Transfer", "SocialRecovery", "RemoveManager", "AddManager"
    };

    public static readonly HashSet<string> AllSupportTypes = new()
    {
        "Transfer", "CrossChainTransfer", "CrossChainReceiveToken", "SocialRecovery", "RemoveManager", "AddManager"
    };
    
    public static readonly List<string> TransferTypes = new()
    {
        "Transfer", "CrossChainTransfer", "CrossChainReceiveToken"
    };
    
    public static readonly List<string> ContractTypes = new()
    {
        "SocialRecovery", "RemoveManager", "AddManager"
    };
}
using System.Collections.Generic;

namespace CAServer.CAActivity;

public static class ActivityConstants
{
    public static readonly Dictionary<string, string> TypeMap = new()
    {
        { "Transfer", "Transfer" },
        { "CrossChainTransfer", "CrossChain Transfer" },
        { "SocialRecovery", "Social Recovery" },
        { "RemoveManagerInfo", "Exit Wallet" },
        { "AddManagerInfo", "Scan code login" },
        { "CreateCAHolder", "Create CA address" },
        { "AddGuardian", "Add guardian" },
        { "RemoveGuardian", "Remove guardian" },
        { "UpdateGuardian", "Edit guardian" },
        { "SetGuardianForLogin", "Set login account" },
        { "UnsetGuardianForLogin", "Unset login account" },
        { "RemoveOtherManagerInfo", "Remove device" },
        { "ClaimToken", "Transfer" },
        { "Register", "Register" },
        { "Approve", "Approve" },
        { "Bingo", "Bingo" },
        { "Play", "Play" }
    };

    public static readonly List<string> DefaultTypes = new()
    {
        "Transfer", "SocialRecovery", "RemoveManagerInfo", "AddManagerInfo"
    };

    public static readonly HashSet<string> AllSupportTypes = new()
    {
        "Transfer", "CrossChainTransfer", "CrossChainReceiveToken", "SocialRecovery", "RemoveManagerInfo",
        "AddManagerInfo", "CreateCAHolder", "AddGuardian", "RemoveGuardian", "UpdateGuardian", "SetGuardianForLogin",
        "UnsetGuardianForLogin", "RemoveOtherManagerInfo", "ClaimToken", "Approve", "Bingo", "Play"
        "Transfer", "CrossChainTransfer", "CrossChainReceiveToken", "SocialRecovery", "RemoveManagerInfo",
        "AddManagerInfo", "CreateCAHolder", "AddGuardian", "RemoveGuardian", "UpdateGuardian", "SetGuardianForLogin",
        "UnsetGuardianForLogin", "RemoveOtherManagerInfo", "ClaimToken", "Register", "Approve", "Bingo", "Play"
    };

    public static readonly List<string> TransferTypes = new()
    {
        "Transfer", "CrossChainTransfer", "CrossChainReceiveToken", "ClaimToken"
    };

    public static readonly List<string> ContractTypes = new()
    {
        "SocialRecovery", "RemoveManagerInfo", "AddManagerInfo", "CreateCAHolder", "AddGuardian", "RemoveGuardian",
        "UpdateGuardian", "SetGuardianForLogin", "UnsetGuardianForLogin", "RemoveOtherManagerInfo", "Approve", "Bingo", "Play"
    };

    public static readonly List<string> ShowPriceTypes = new()
    {
        "Transfer", "CrossChainTransfer", "CrossChainReceiveToken", "RemoveManagerInfo",
        "AddManagerInfo", "AddGuardian", "RemoveGuardian", "UpdateGuardian", "SetGuardianForLogin",
        "UnsetGuardianForLogin", "RemoveOtherManagerInfo", "ClaimToken", "Approve", "Bingo", "Play"
        "SocialRecovery", "RemoveManagerInfo", "AddManagerInfo", "CreateCAHolder", "AddGuardian", "RemoveGuardian",
        "UpdateGuardian", "SetGuardianForLogin", "UnsetGuardianForLogin", "RemoveOtherManagerInfo", "Register",
        "Approve", "Bingo",
        "Play"
    };

    public static readonly List<string> ShowPriceTypes = new()
    {
        "Transfer", "CrossChainTransfer", "CrossChainReceiveToken", "RemoveManagerInfo",
        "AddManagerInfo", "AddGuardian", "RemoveGuardian", "UpdateGuardian", "SetGuardianForLogin",
        "UnsetGuardianForLogin", "RemoveOtherManagerInfo", "ClaimToken", "Approve", "Bingo", "Play"
    };

    public static readonly List<string> ShowNftTypes = new()
    {
        "Transfer", "CrossChainTransfer"
    };

    public static readonly List<string> RecentTypes = new()
    {
        "Transfer", "CrossChainTransfer", "ClaimToken"
    };

    public static readonly string Zero = "0";
}
namespace CAServer.Contract;

public class MethodName
{
    public const string CreateCAHolder = "CreateCAHolder";
    public const string SocialRecovery = "SocialRecovery";
    public const string Validate = "ValidateCAHolderInfoWithManagersExists";
    public const string UpdateMerkleTree = "GetBoundParentChainHeightAndMerklePathByHeight";
    public const string SyncHolderInfo = "SyncHolderInfo";
    public const string GetParentChainHeight = "GetParentChainHeight";
    public const string GetSideChainHeight = "GetSideChainHeight";
    public const string GetHolderInfo = "GetHolderInfo";
}

public static class ContractName
{
    public const string CrossChain = "AElf.ContractNames.CrossChain";
}
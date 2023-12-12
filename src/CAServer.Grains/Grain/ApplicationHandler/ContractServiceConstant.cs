namespace CAServer.Grains.Grain.ApplicationHandler;

public static class MethodName
{
    public const string CreateCAHolder = "CreateCAHolder";
    public const string SocialRecovery = "SocialRecovery";
    public const string Validate = "ValidateCAHolderInfoWithManagerInfosExists";
    public const string UpdateMerkleTree = "GetBoundParentChainHeightAndMerklePathByHeight";
    public const string SyncHolderInfo = "SyncHolderInfo";
    public const string GetParentChainHeight = "GetParentChainHeight";
    public const string GetSideChainHeight = "GetSideChainHeight";
    public const string GetHolderInfo = "GetHolderInfo";
    public const string CreateRedPacket = "CreateRedPacket";
    public const string TransferRedPacket = "TransferCryptoBox";
    public const string RefundRedPacket = "RefundCryptoBox";

}

public static class TransactionState
{
    public const string Mined = "MINED";
    public const string Pending = "PENDING";
    public const string NotExisted = "NOTEXISTED";
    public const string Failed = "FAILED";
    public const string NodeValidationFailed = "NODEVALIDATIONFAILED";
}
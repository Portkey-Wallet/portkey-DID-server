namespace CAServer.RedPackage;

public class RedPackageConsts
{
    // public static int ExpireTimeMs = 60 * 60 * 24 * 1000;
    public static int ExpireTimeMs = 60 * 3  * 1000;
    public static string RedPackageCardType = "REDPACKAGE-CARD";
    public static int MaxRedPackageGrabberCount = 100;
    public static int DefaultRedPackageGrabberCount = 10;
    
    //validate error
    public const string UserNotExist = "User Not Exist";
    public const string RedPackageNotExist = "RedPackage Not Exist";
    public const string RedPackageIdInvalid = "RedPackageId is Invalid";
    public const string RedPackageCountSmallError = "RedPackage count should bigger than 0";
    public const string RedPackageAmountError = "RedPackage amount too small";
    public const string RedPackageCountBigError = "RedPackage count too large";
    public const string RedPackageTypeError = "Unsupported RedPackage Type";
    public const string RedPackageChainError = "Unsupported RedPackage chain or symbol";
    public const string RedPackageTransactionError = "Transaction should not empty";
    public const string RedPackageKeyError = "RedPackage key is empty";
    public const string RedPackageChannelError = "Channel should not empty";
    
    //grab error
    public const string RedPackageFullyClaimed = "Red packets have been FullyClaimed";
    public const string RedPackageExpired = "RedPackage has Expired";
    public const string RedPackageCancelled = "RedPackage has been Cancelled";
    public const string RedPackageNotSet = "RedPackage has not been Init";
    public const string RedPackageUserGrabbed = "User has Grabbed before";
    public const string RedPackageGrabbedByOthers = "User has Grabbed by others";
}

public static class TransferConstant
{
    public static string TransferCardType = "TRANSFER-CARD";
}
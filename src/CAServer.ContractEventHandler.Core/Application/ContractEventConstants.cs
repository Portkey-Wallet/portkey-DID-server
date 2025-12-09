namespace CAServer.ContractEventHandler.Core.Application;

public class ContractEventConstants
{
    public const string SyncHolderUpdateVersionCachePrefix = "SyncHolderUpdateVersion";
    public const long SyncHolderUpdateVersionCacheExpireTime = 60 * 60 * 24;
    
    public const string BlockHeightCachePrefix = "BlockHeight";
    public const int BlockHeightCacheExpireMinutes = 30;
}
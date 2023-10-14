namespace CAServer.Commons;

public enum MonitorRequestType
{
    Relation,
    GetIpInfo,
    IsGoogleRecaptchaTokenValid,
    CoinGeckoPro,
    AppleAuth
}

public enum MonitorAelfClientType
{
    SendTransactionAsync,
    GetTransactionResultAsync,
    GetChainStatusAsync,
    GetContractFileDescriptorSetAsync,
    GenerateTransactionAsync,
    GetMerklePathByTransactionIdAsync,
    ExecuteTransactionAsync
}

public enum RegisterAndRecoveryLogType
{
    CheckCaHolderExists,
    CreateCaHolder,
    SocialRecovery,
    GetHolderInfo
}

public enum DataSyncLogType
{
    QueryRecord,
    ValidateTransaction,
    CheckBlockIndex,
    GetHolderInfo,
    SyncTransaction
}
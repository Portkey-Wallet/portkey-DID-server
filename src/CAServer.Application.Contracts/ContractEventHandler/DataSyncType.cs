namespace CAServer.ContractEventHandler;

public enum DataSyncType
{
    RegisterChainBlock,
    GetRecord,
    EndValidate,
    BeginSync,
    EndSync,
    SyncChainBlock
}
using System.Collections.Generic;

namespace CAServer.ContractEventHandler.Core.Application;

public class ChainOptions
{
    public Dictionary<string, ChainInfo> ChainInfos { get; set; }
}

public class ChainInfo
{
    public string ChainId { get; set; }
    public string BaseUrl { get; set; }
    public string ContractAddress { get; set; }
    public string TokenContractAddress { get; set; }
    public string CrossChainContractAddress { get; set; }
    public string PublicKey { get; set; }
    public bool IsMainChain { get; set; }
}

public class IndexOptions
{
    public int IndexDelay { get; set; }
    public long IndexInterval { get; set; }
    public long IndexSafe { get; set; }
    public long IndexTimes { get; set; }
    public int MaxRetryTimes { get; set; }
    public int MaxBucket { get; set; }
    public Dictionary<string, long> AutoSyncStartHeight { get; set; } = new()
    {
        { "AELF", 146928049 },
        { "tDVV", 139522008 }
    };
}

public class GraphQLOptions
{
    public string GraphQLConnection { get; set; }
}

public class ContractSyncOptions
{
    public int Sync { get; set; }
    public int AutoReceive { get; set; }
}
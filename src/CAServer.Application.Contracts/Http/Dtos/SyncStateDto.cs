using System.Collections.Generic;
using CAServer.Commons.Etos;

namespace CAServer.Http.Dtos;

public class SyncStateDto
{
    public  IndexerVersionInfo CurrentVersion { get; set; }
}

public class IndexerVersionInfo
{
    public List<SyncStateItem> Items { get; set; } = new();
}

public class SyncStateItem : ChainDisplayNameDto
{
    public string LongestChainBlockHash { get; set; }
    public long LongestChainHeight { get; set; }
    public string BestChainBlockHash { get; set; }
    public long BestChainHeight { get; set; }
    public string LastIrreversibleBlockHash { get; set; }
    public long LastIrreversibleBlockHeight { get; set; }
}
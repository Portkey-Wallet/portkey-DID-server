namespace CAServer.Common;

public class ChainHeightCache
{
    public long SideChainIndexHeight { get; set; }
    public long ParentChainHeight { get; set; }
    public long MainChainBlockHeight { get; set; }
}
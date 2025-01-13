using System.Collections.Generic;

namespace CAServer.UserAssets.Provider;

public class IndexerTokenInfos
{
    public CaHolderTokenBalanceInfo CaHolderTokenBalanceInfo { get; set; }
}

public class CaHolderTokenBalanceInfo
{
    public List<IndexerTokenInfo> Data { get; set; }
    public long totalRecordCount { get; set; }
}

public class IndexerTokenInfo
{
    public string CaAddress { get; set; }
    public string ChainId { get; set; }
    public long Balance { get; set; }
    public List<long> TokenIds { get; set; }
    public TokenInfo TokenInfo { get; set; }
}

public class TokenInfo
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string TokenContractAddress { get; set; }
    public int Decimals { get; set; }
    public string TokenName { get; set; }
    public long TotalSupply { get; set; }
    public string ImageUrl { get; set; }
}

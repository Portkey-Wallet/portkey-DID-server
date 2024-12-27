using System.Collections.Generic;

namespace CAServer.Tokens.Provider;

public class IndexerTokens
{
    public List<IndexerToken> TokenInfo { get; set; }
}

public class IndexerToken
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public int BlockHeight { get; set; }
    public string Symbol { get; set; }
    public string Type { get; set; }
    public string TokenContractAddress { get; set; }
    public int Decimals { get; set; }
    public string TokenName { get; set; }
    public long TotalSupply { get; set; }
    public string Issuer { get; set; }
    public bool IsBurnable { get; set; }
    public long IssueChainId { get; set; }
    public string ImageUrl { get; set; }
}
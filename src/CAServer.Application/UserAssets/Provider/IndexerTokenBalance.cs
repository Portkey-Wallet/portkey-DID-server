using System.Collections.Generic;

namespace CAServer.UserAssets.Provider;

public class IndexerTokenBalance
{
    public List<TokenBalance> CaHolderTokenBalanceInfo { get; set; }
}

public class TokenBalance
{
    public string ChainId { get; set; }

    public IndexerTokenInfo IndexerTokenInfo { get; set; }
    public long Balance { get; set; }
}

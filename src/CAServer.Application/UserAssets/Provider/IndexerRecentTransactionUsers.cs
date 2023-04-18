using System.Collections.Generic;

namespace CAServer.UserAssets.Provider;

public class IndexerRecentTransactionUsers
{
    public List<CAHolderTransactionAddress> CaHolderTransactionAddressInfo { get; set; }
}

public class CAHolderTransactionAddress
{
    public string ChainId { get; set; }
    public string CaAddress { get; set; }
    public long TransactionTime { get; set; }
}
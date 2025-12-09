using System.Collections.Generic;

namespace CAServer.UserAssets.Provider;

public class IndexerRecentTransactionUsers
{
    public CaHolderTransactionAddressInfo CaHolderTransactionAddressInfo { get; set; }
}

public class CaHolderTransactionAddressInfo
{
    public List<CAHolderTransactionAddress> Data { get; set; }
    public long TotalRecordCount { get; set; }
}

public class CAHolderTransactionAddress
{
    public string ChainId { get; set; }
    public string CaAddress { get; set; }
    public long TransactionTime { get; set; }
    public string Address { get; set; }
    public string AddressChainId { get; set; }
}
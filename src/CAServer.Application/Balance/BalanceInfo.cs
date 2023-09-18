using System.Collections.Generic;

namespace CAServer.Balance;

public class BalanceInfo
{
    public string ChainId { get; set; }
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public long Balance { get; set; }
}

public class BalanceInfoItem
{
    public string ChainId { get; set; }
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
}
using System.Collections.Generic;

namespace CAServer.Options;

public class SecurityOptions
{
    public Dictionary<string, TokenTransferLimit> TokenTransferLimitDict { get; set; }
    public Dictionary<string, long> TokenBalanceTransferThreshold { get; set; }
    public long DefaultTokenTransferLimit { get; set; }
    public Dictionary<string, long> DefaultTokenDecimalDict { get; set; }
    public long DefaultTokenDecimals { get; set; } = 8;
}

public class TokenTransferLimit
{
    public Dictionary<string, long> SingleTransferLimit { get; set; }
    public Dictionary<string, long> DailyTransferLimit { get; set; }
}
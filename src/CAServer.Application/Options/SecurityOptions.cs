using System.Collections.Generic;

namespace CAServer.Options;

public class SecurityOptions
{
    public Dictionary<string, TokenTransferLimit> TokenTransferLimitDict { get; set; }
    public Dictionary<string, long> TokenBalanceTransferThreshold { get; set; }
    public long DefaultTokenTransferLimit { get; set; }
}

public class TokenTransferLimit
{
    public Dictionary<string, string> SingleTransferLimit { get; set; }
    public Dictionary<string, string> DailyTransferLimit { get; set; }
}
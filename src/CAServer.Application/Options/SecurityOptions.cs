using System.Collections.Generic;

namespace CAServer.Options;

public class SecurityOptions
{
    public Dictionary<string, TokenTransferLimit> TokenTransferLimitDict { get; set; }
    public Dictionary<string, float> TokenBalanceTransferThreshold { get; set; }
    public long DefaultTokenTransferLimit { get; set; }
}

public class TokenTransferLimit
{
    public Dictionary<string, float> SingleTransferLimit { get; set; }
    public Dictionary<string, float> DailyTransferLimit { get; set; }
}
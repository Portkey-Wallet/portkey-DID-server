using System.Collections.Generic;

namespace CAServer.Options;

public class SecurityOptions
{
    public Dictionary<string, TokenTransferLimit> TokenTransferLimitDict { get; set; }
    public Dictionary<string, long> TokenBalanceTransferThreshold { get; set; }
    public float DefaultTokenTransferLimit { get; set; }
}

public class TokenTransferLimit
{
    public Dictionary<string, float> SingleTransferLimit { get; set; }
    public Dictionary<string, float> DailyTransferLimit { get; set; }
}
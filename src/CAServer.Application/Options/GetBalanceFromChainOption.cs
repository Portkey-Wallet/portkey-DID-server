using System.Collections.Generic;

namespace CAServer.Options;

public class GetBalanceFromChainOption
{
    public bool IsOpen { get; set; }
    public List<string> Symbols { get; set; }
    public long ExpireSeconds { get; set; }
}
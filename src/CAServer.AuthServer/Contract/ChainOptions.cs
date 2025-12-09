using System.Collections.Generic;

namespace CAServer.Contract;

public class ChainOptions
{
    public Dictionary<string, ChainInfo> ChainInfos { get; set; }
}

public class GrainOptions
{
    public int Delay { get; set; }
    public int RetryDelay { get; set; }
    public int RetryTimes { get; set; }
}
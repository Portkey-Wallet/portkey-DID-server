namespace CAServer.Grains.Grain.ApplicationHandler;

public class ChainOptions
{
    public Dictionary<string, ChainInfo> ChainInfos { get; set; }
}

public class GrainOptions
{
    public int Delay { get; set; }
    public int RetryDelay { get; set; }
    public int RetryTimes { get; set; }
    public long SafeBlockHeight { get; set; }
}

public class SignatureOptions
{
    public string BaseUrl { get; set; }
}
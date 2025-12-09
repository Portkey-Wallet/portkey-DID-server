using System.Collections.Generic;

namespace CAServer.Options;

public class TokenPriceWorkerOption
{
    public string Prefix { get; set; } = "TokenPrice";

    //s
    public int Period { get; set; } = 300;
    public string WorkerNameKey { get; set; } = "Worker";
    public string WorkerLockKey { get; set; } = "Lock";
    public string PricePrefix { get; set; } = "Price";

    public IEnumerable<string> Symbols { get; set; }
}
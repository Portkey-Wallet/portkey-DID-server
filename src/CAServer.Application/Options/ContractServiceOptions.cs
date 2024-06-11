namespace CAServer.Options;

public class ContractServiceOptions
{
    public int Delay { get; set; } = 2000;
    public int RetryDelay { get; set; } = 2000;
    public int RetryTimes { get; set; } = 10;
    public int CryptoBoxRetryDelay { get; set; }
    public int CryptoBoxRetryTimes { get; set; }
    public long SafeBlockHeight { get; set; }
    public bool UseGrainService { get; set; } = false;
}
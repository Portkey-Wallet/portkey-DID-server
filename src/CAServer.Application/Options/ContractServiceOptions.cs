namespace CAServer.Options;

public class ContractServiceOptions
{
    public int Delay { get; set; }
    public int RetryDelay { get; set; }
    public int RetryTimes { get; set; }
    public int CryptoBoxRetryDelay { get; set; }
    public int CryptoBoxRetryTimes { get; set; }
    public long SafeBlockHeight { get; set; }
    public bool UseGrainService { get; set; } = false;
}
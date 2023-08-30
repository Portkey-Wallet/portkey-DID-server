namespace CAServer.BackGround.Options;

public class TransactionOptions
{
    public int DelayTime { get; set; }
    public int RetryTime { get; set; }
    public string SendToChainId { get; set; }
    public string RecurringPeriod { get; set; }
    public string NftOrderMerchantCallbackPeriod { get; set; }
    public string NftOrderThirdPartResultPeriod { get; set; }
    public long ResendTimeInterval { get; set; }
}
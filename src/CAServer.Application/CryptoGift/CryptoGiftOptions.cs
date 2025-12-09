namespace CAServer.CryptoGift;

public class CryptoGiftOptions
{
    public int ExpireSeconds { get; set; }

    public int TransferDelayedRetryTimes { get; set; } = 50;

    public int TransferDelayedIntervalSeconds { get; set; } = 5;
}
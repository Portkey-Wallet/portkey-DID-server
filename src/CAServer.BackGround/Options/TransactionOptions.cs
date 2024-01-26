namespace CAServer.BackGround.Options;

public class TransactionOptions
{
    public int DelayTime { get; set; }
    public int RetryTime { get; set; }
    public string SendToChainId { get; set; }
    public string RecurringPeriod { get; set; }
    public string NftOrderMerchantCallbackPeriod { get; set; } = "0 0/15 * * * ?";
    public string NftOrderThirdPartResultPeriod { get; set; } = "0 5/15 * * * ?";
    public string HandleUnCompletedNftOrderPayResultPeriod { get; set; } = "0 10/15 * * * ?";
    public string HandleUnCompletedNftOrderPayTransferPeriod { get; set; } = "0/15 * * * * ?";
    public long ResendTimeInterval { get; set; }
    public string LockKeyPrefix { get; set; } = "CAServer.BGD:NFT_Order_worker:";
    public string NftOrdersSettlementPeriod { get; set; } = "0 0 0/1 * * ?";
    public string HandleUnCompletedTreasuryTransferPeriod { get; set; } = "0/15 * * * * ?";
    public string HandleUnCompletedTreasuryCallbackPeriod { get; set; } = "0 0/1 * * * ?";
    public string HandlePendingTreasuryOrderPeriod { get; set; } = "0/15 * * * * ?";
}
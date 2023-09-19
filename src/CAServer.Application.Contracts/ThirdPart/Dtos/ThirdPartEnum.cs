namespace CAServer.ThirdPart.Dtos;

public enum MerchantNameType
{
    Alchemy = 0,
    Unknown
}

public enum TransferDirectionType
{
    TokenBuy,
    TokenSell,
    NFTBuy,
    NFTSell
}

public enum OrderStatusType
{
    Unknown,
    Initialized,
    Created,
    Invalid,
    Canceled,
    Expired,
    Finish,
    Failed,
    Pending,
    Refunded,
    StartTransfer,
    Transferring,
    Transferred,
    TransferFailed,
    UserCompletesCoinDeposit,
    StartPayment,
    SuccessfulPayment,
    PaymentFailed,
    RefundSuccessfully
}

public enum OrderTransDirect
{
    BUY,
    SELL
}
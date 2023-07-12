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
    Created,
    Canceled,
    Expired,
    Finish,
    Failed,
    Pending,
    Refunded,
    Unknown,
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
namespace MockServer.Dtos;

public enum TransferDirectionType
{
    BUY,
    SELL,
}

public enum OrderStatusType
{
    Created,
    Finish,
    Failed,
    StartPayment,
    SuccessfulPayment,
    PaymentFailed,
}
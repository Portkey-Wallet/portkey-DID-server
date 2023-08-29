using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos;

public enum ThirdPartNameType
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


public enum OrderTransDirect
{
    BUY,
    SELL
}

public enum NftOrderWebhookStatus
{
    NONE,
    SUCCESS,
    FAIL,
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

public class OrderStatusTransitions
{
    public static bool Reachable(OrderStatusType from, OrderStatusType to)
    {
        return _reachableDict.GetValueOrDefault(from, Empty).Contains(to);
    }
    
    private static readonly List<OrderStatusType> Empty = new List<OrderStatusType>();
    
    // currentStatus => reachable next status list
    private static Dictionary<OrderStatusType, List<OrderStatusType>> _reachableDict = new()
    {
        [OrderStatusType.Invalid] = Empty,
        [OrderStatusType.Canceled] = Empty,
        [OrderStatusType.Expired] = Empty,
        [OrderStatusType.Finish] = Empty,
        [OrderStatusType.Failed] = Empty,
        [OrderStatusType.TransferFailed] = Empty,
        [OrderStatusType.PaymentFailed] = Empty,
        [OrderStatusType.RefundSuccessfully] = Empty,
        
        // by webhook: off-ramp, pay fiat to user success
        [OrderStatusType.SuccessfulPayment] = Empty,
        
        // by local: order create locally
        [OrderStatusType.Initialized] = new()
        {
            OrderStatusType.Created,
            OrderStatusType.Invalid,
            OrderStatusType.Canceled,
            OrderStatusType.Expired,
            OrderStatusType.Finish,
            OrderStatusType.Failed,
            OrderStatusType.Pending,
            OrderStatusType.Refunded,
            OrderStatusType.StartTransfer,
            OrderStatusType.Transferring,
            OrderStatusType.Transferred,
            OrderStatusType.TransferFailed,
            OrderStatusType.UserCompletesCoinDeposit,
            OrderStatusType.StartPayment,
            OrderStatusType.SuccessfulPayment,
            OrderStatusType.PaymentFailed,
            OrderStatusType.RefundSuccessfully,
        },
        
        // by webhook : on-ramp user pay fiat success
        [OrderStatusType.Created] =  new()
        {
            OrderStatusType.Invalid,
            OrderStatusType.Canceled,
            OrderStatusType.Expired,
            OrderStatusType.Finish,
            OrderStatusType.Failed,
            OrderStatusType.Pending,
            OrderStatusType.Refunded,
            OrderStatusType.StartTransfer,
            OrderStatusType.Transferring,
            OrderStatusType.Transferred,
            OrderStatusType.TransferFailed,
            OrderStatusType.UserCompletesCoinDeposit,
            OrderStatusType.StartPayment,
            OrderStatusType.SuccessfulPayment,
            OrderStatusType.PaymentFailed,
            OrderStatusType.RefundSuccessfully,
        },
        
        // on-ramp user pay fiat success
        [OrderStatusType.Pending] =  new()
        {
            OrderStatusType.Refunded,
            OrderStatusType.StartTransfer,
            OrderStatusType.Transferring,
            OrderStatusType.Transferred,
            OrderStatusType.TransferFailed,
            OrderStatusType.UserCompletesCoinDeposit,
            OrderStatusType.StartPayment,
            OrderStatusType.SuccessfulPayment,
            OrderStatusType.PaymentFailed,
            OrderStatusType.RefundSuccessfully,
        },
        
        // off-ramp order transfer just start
        [OrderStatusType.StartTransfer] =  new()
        {
            OrderStatusType.Transferring,
            OrderStatusType.Transferred,
            OrderStatusType.TransferFailed,
            OrderStatusType.UserCompletesCoinDeposit,
            OrderStatusType.StartPayment,
            OrderStatusType.SuccessfulPayment,
            OrderStatusType.PaymentFailed,
        },
        
        // off-ramp, user transferred crypto, waiting for resout 
        [OrderStatusType.Transferring] =  new()
        {
            OrderStatusType.Transferred,
            OrderStatusType.TransferFailed,
            OrderStatusType.UserCompletesCoinDeposit,
            OrderStatusType.StartPayment,
            OrderStatusType.SuccessfulPayment,
            OrderStatusType.PaymentFailed,
        },
        
        // by local transaction: off-ramp user transfer crypto success
        [OrderStatusType.Transferred] =  new()
        {
            OrderStatusType.UserCompletesCoinDeposit,
            OrderStatusType.StartPayment,
            OrderStatusType.SuccessfulPayment,
            OrderStatusType.PaymentFailed,
        },
        
        // by third webhook : off-ramp user transfer crypto complete
        [OrderStatusType.UserCompletesCoinDeposit] =  new()
        {
            OrderStatusType.StartPayment,
            OrderStatusType.SuccessfulPayment,
            OrderStatusType.PaymentFailed,
        },
        
        // by webhook : off-ramp thirdPart start pay fiat to user
        [OrderStatusType.StartPayment] =  new()
        {
            OrderStatusType.SuccessfulPayment,
            OrderStatusType.PaymentFailed,
        },
        [OrderStatusType.Refunded] =  new()
        {
            OrderStatusType.RefundSuccessfully,
        },
    };
}
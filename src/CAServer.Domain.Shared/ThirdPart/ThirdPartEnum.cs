using System.Collections.Generic;
using System.Linq;

namespace CAServer.ThirdPart;

public enum ThirdPartNameType
{
    Alchemy = 0,
    Transak = 1,
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

public enum NftReleaseResult
{
    SUCCESS,
    FAIL,
}

public enum NftOrderWebhookStatus
{
    NONE,
    SUCCESS,
    FAIL,
}

public static class OrderDisplayStatus
{
    private const string PaySuccess = "PaySuccess";
    
    
    private static Dictionary<string, List<OrderStatusType>> _onRampStatusMapping = new()
    {
        [OrderStatusType.Initialized.ToString()] = new List<OrderStatusType> { OrderStatusType.Initialized },
        [OrderStatusType.Created.ToString()] = new List<OrderStatusType> { OrderStatusType.Created },
        [PaySuccess] = new List<OrderStatusType> { OrderStatusType.Pending },
        [OrderStatusType.Finish.ToString()] = new List<OrderStatusType> { OrderStatusType.Finish },
        [OrderStatusType.Failed.ToString()] = new List<OrderStatusType> { OrderStatusType.Failed, OrderStatusType.Expired},
    };
    

    private static Dictionary<string, List<OrderStatusType>> _offRampStatusMapping = new()
    {
        [OrderStatusType.Initialized.ToString()] = new List<OrderStatusType> { OrderStatusType.Initialized },
        [OrderStatusType.Created.ToString()] = new List<OrderStatusType> { OrderStatusType.Created },
        [OrderStatusType.Transferring.ToString()] = new List<OrderStatusType> { OrderStatusType.StartTransfer, OrderStatusType.Transferring, OrderStatusType.Transferred, },
        [OrderStatusType.Transferred.ToString()] = new List<OrderStatusType> { OrderStatusType.UserCompletesCoinDeposit },
        [OrderStatusType.StartPayment.ToString()] = new List<OrderStatusType> { OrderStatusType.StartPayment },
        [OrderStatusType.Finish.ToString()] = new List<OrderStatusType> { OrderStatusType.Finish },
        [OrderStatusType.Failed.ToString()] = new List<OrderStatusType> { OrderStatusType.Failed, OrderStatusType.TransferFailed, OrderStatusType.PaymentFailed, OrderStatusType.Expired}
    };
    
    private static Dictionary<string, List<OrderStatusType>> _nftCheckoutStatusMapping = new()
    {
        [OrderStatusType.Initialized.ToString()] = new List<OrderStatusType> { OrderStatusType.Initialized },
        [OrderStatusType.Created.ToString()] = new List<OrderStatusType> { OrderStatusType.Created },
        [PaySuccess] = new List<OrderStatusType> { OrderStatusType.Pending },
        [OrderStatusType.Transferring.ToString()] = new List<OrderStatusType> { OrderStatusType.StartTransfer, OrderStatusType.Transferring, },
        [OrderStatusType.Transferred.ToString()] = new List<OrderStatusType> { OrderStatusType.Transferred },
        [OrderStatusType.Finish.ToString()] = new List<OrderStatusType> { OrderStatusType.Finish },
        [OrderStatusType.Failed.ToString()] = new List<OrderStatusType> { OrderStatusType.Failed, OrderStatusType.TransferFailed, OrderStatusType.Expired}
    };

    public static string ToOnRampDisplayStatus(string orderStatus)
    {
        return ToDisplayStatus(orderStatus, _onRampStatusMapping);
    }
    
    public static string ToOffRampDisplayStatus(string orderStatus)
    {
        return ToDisplayStatus(orderStatus, _offRampStatusMapping);
    }
    
    public static string ToNftCheckoutRampDisplayStatus(string orderStatus)
    {
        return ToDisplayStatus(orderStatus, _nftCheckoutStatusMapping);
    }

    private static string ToDisplayStatus(string orderStatus, Dictionary<string, List<OrderStatusType>> statusMapping)
    {
        return statusMapping
            .Where(kv => kv.Value.Any(v => v.ToString() == orderStatus))
            .Select(kv => kv.Key)
            .FirstOrDefault(orderStatus);
    }
    
}

/// <summary>
/// Order status transitions:
/// 
///     on-ramp:   Initialized - Created - Pending - Finish
///                                     \- Failed
/// 
///     off-ramp:  Initialized - Created - StartTransfer - Transferring - Transferred - UserCompletesCoinDeposit - StartPayment - SuccessfulPayment
///                                                                    \- TransferFailed                                       \- PaymentFailed
/// 
///     nft-buy:   Initialized - Created - Pending - StartTransfer - Transferring - Transferred - Finish
///                                   \      \-(user pay success)                \- TransferFailed
///                                    \-  Failed,Expired
/// </summary>
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
        return from == to || _reachableDict.GetValueOrDefault(from, Empty).Contains(to);
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
        [OrderStatusType.Created] = new()
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
        [OrderStatusType.Pending] = new()
        {
            OrderStatusType.Canceled,
            OrderStatusType.Expired,
            OrderStatusType.Finish,
            OrderStatusType.Failed,
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
        // nft-checkout order pay success
        [OrderStatusType.StartTransfer] = new()
        {
            OrderStatusType.Expired,
            OrderStatusType.Finish,
            OrderStatusType.Failed,
            OrderStatusType.Refunded,
            OrderStatusType.Transferring,
            OrderStatusType.Transferred,
            OrderStatusType.TransferFailed,
            OrderStatusType.UserCompletesCoinDeposit,
            OrderStatusType.StartPayment,
            OrderStatusType.SuccessfulPayment,
            OrderStatusType.PaymentFailed,
            OrderStatusType.RefundSuccessfully,
        },

        // off-ramp, user transferred crypto, waiting for resout 
        [OrderStatusType.Transferring] = new()
        {
            OrderStatusType.Transferred,
            OrderStatusType.TransferFailed,
            OrderStatusType.UserCompletesCoinDeposit,
            OrderStatusType.StartPayment,
            OrderStatusType.SuccessfulPayment,
            OrderStatusType.PaymentFailed,
            OrderStatusType.Finish,
            OrderStatusType.Failed,
        },

        // by local transaction: off-ramp user transfer crypto success
        [OrderStatusType.Transferred] = new()
        {
            OrderStatusType.TransferFailed, // transaction rollback
            OrderStatusType.UserCompletesCoinDeposit,
            OrderStatusType.StartPayment,
            OrderStatusType.SuccessfulPayment,
            OrderStatusType.PaymentFailed,
            OrderStatusType.Finish,
            OrderStatusType.Failed,
        },
        
        [OrderStatusType.TransferFailed] = new()
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
            OrderStatusType.Transferred
        },

        // by third webhook : off-ramp user transfer crypto complete
        [OrderStatusType.UserCompletesCoinDeposit] = new()
        {
            OrderStatusType.StartPayment,
            OrderStatusType.SuccessfulPayment,
            OrderStatusType.PaymentFailed,
        },

        // by webhook : off-ramp thirdPart start pay fiat to user
        [OrderStatusType.StartPayment] = new()
        {
            OrderStatusType.SuccessfulPayment,
            OrderStatusType.PaymentFailed,
        },
        [OrderStatusType.Refunded] = new()
        {
            OrderStatusType.RefundSuccessfully,
        },
    };
}
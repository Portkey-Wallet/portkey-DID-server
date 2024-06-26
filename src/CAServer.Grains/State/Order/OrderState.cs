namespace CAServer.Grains.State.Order;

public class OrderState
{
    // common parameters
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ThirdPartOrderNo { get; set; }
    public string MerchantName { get; set; }
    public string TransDirect { get; set; }
    public string Address { get; set; }
    public string Crypto { get; set; }
    public string CryptoPrice { get; set; }
    public string CryptoAmount { get; set; }
    public int CryptoDecimals { get; set; }
    public string Network { get; set; }
    public string Fiat { get; set; }
    public string FiatAmount { get; set; }
    public string Status { get; set; }
    public string ThirdPartCrypto { get; set; }
    public string ThirdPartNetwork { get; set; }

    // buy direction order
    public string CryptoQuantity { get; set; }
    public string PaymentMethod { get; set; }
    public string TxTime { get; set; }

    // sell direction order
    public string ReceivingMethod { get; set; }
    public string ReceiptTime { get; set; }
    
    public string LastModifyTime { get; set; }
    public bool IsDeleted { get; set; } = false;
    
    public string TransactionId { get; set; }
    
    public string RawTransaction { get; set; }
}
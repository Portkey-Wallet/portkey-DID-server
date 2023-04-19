using System;
using Volo.Abp.EventBus;

namespace CAServer.ThirdPart.Etos;

[EventName("OrderEto")]
public class OrderEto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string MerchantName { get; set; }
    public string TransDirect { get; set; }
    public string Address { get; set; }
    public string Crypto { get; set; }
    public string CryptoPrice { get; set; }
    public string Fiat { get; set; }
    public string FiatAmount { get; set; }
    public string Status { get; set; }
    public string LastModifyTime { get; set; }
    public bool IsDeleted { get; set; } = false;

    // buy order
    public string CryptoQuantity { get; set; }
    public string PaymentMethod { get; set; }
    public string TxTime { get; set; }

    // sell order
    public string ReceivingMethod { get; set; }
    public string ReceiptTime { get; set; }
}
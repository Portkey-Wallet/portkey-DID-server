namespace CAServer.Grains.Grain.ThirdPart;

[GenerateSerializer]
public class OrderGrainDto
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public Guid UserId { get; set; }
    [Id(2)]
    public string ThirdPartOrderNo { get; set; }
    [Id(3)]
    public string MerchantName { get; set; }

    [Id(4)]
    public string TransDirect { get; set; }
    [Id(5)]
    public string Address { get; set; }
    [Id(6)]
    public string Crypto { get; set; }
    [Id(7)]
    public string CryptoPrice { get; set; }
    [Id(8)]
    public string CryptoAmount { get; set; }
    [Id(9)]
    public int CryptoDecimals { get; set; }
    [Id(10)]
    public string Fiat { get; set; }
    [Id(11)]
    public string FiatAmount { get; set; }
    [Id(12)]
    public string LastModifyTime { get; set; }
    [Id(13)]
    public string Network { get; set; }
    [Id(14)]
    public string Status { get; set; }

    [Id(15)]
    public string ThirdPartCrypto { get; set; }
    [Id(16)]
    public string ThirdPartNetwork { get; set; }

    // buy order
    [Id(17)]
    public string CryptoQuantity { get; set; }
    [Id(18)]
    public string PaymentMethod { get; set; }
    [Id(19)]
    public string TxTime { get; set; }

    // sell order
    [Id(20)]
    public string ReceivingMethod { get; set; }
    [Id(21)]
    public string ReceiptTime { get; set; }

    [Id(22)]
    public string TransactionId { get; set; }

    [Id(23)]
    public string RawTransaction { get; set; }
}
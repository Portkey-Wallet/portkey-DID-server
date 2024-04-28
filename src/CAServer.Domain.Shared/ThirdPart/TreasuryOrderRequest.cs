namespace CAServer.ThirdPart;

public class TreasuryOrderRequest
{
    public string ThirdPartName { get; set; }
    public string ThirdPartOrderId { get; set; }

    /// <see cref="TransferDirectionType"/>
    public string TransferDirection { get; set; } = TransferDirectionType.TokenBuy.ToString();
    public string Network { get; set; }
    public string ThirdPartNetwork { get; set; }
    public string ThirdPartCrypto { get; set; }
    public string Address { get; set; }
    public string Crypto { get; set; }
    public string CryptoAmount { get; set; }
    public string CryptoPrice { get; set; }
    public string UsdtAmount { get; set; }
}
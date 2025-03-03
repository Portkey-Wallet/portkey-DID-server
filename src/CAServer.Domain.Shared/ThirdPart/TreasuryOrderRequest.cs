using Orleans;

namespace CAServer.ThirdPart;

[GenerateSerializer]
public class TreasuryOrderRequest
{
    [Id(0)]
    public string ThirdPartName { get; set; }
    [Id(1)]
    public string ThirdPartOrderId { get; set; }

    /// <see cref="TransferDirectionType"/>
    [Id(2)]
    public string TransferDirection { get; set; } = TransferDirectionType.TokenBuy.ToString();
    [Id(3)]
    public string Network { get; set; }
    [Id(4)]
    public string ThirdPartNetwork { get; set; }
    [Id(5)]
    public string ThirdPartCrypto { get; set; }
    [Id(6)]
    public string Address { get; set; }
    [Id(7)]
    public string Crypto { get; set; }
    [Id(8)]
    public string CryptoAmount { get; set; }
    [Id(9)]
    public string CryptoPrice { get; set; }
    [Id(10)]
    public string UsdtAmount { get; set; }
}
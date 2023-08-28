using System.ComponentModel.DataAnnotations;

namespace CAServer.ThirdPart.Dtos;

public class NftMerchantBaseDto
{
    public string MerchantName { get; set; }
    public string Signature { get; set; }
}

// create order
public class CreateNftOrderRequestDto : NftMerchantBaseDto
{
    [Required] public string NftSymbol { get; set; }
    [Required] public string MerchantOrderId { get; set; }
    [Required] public string WebhookUrl { get; set; }
    [Required] public string NftPicture { get; set; }
    [Required] public string PriceSymbol { get; set; }
    [Required] public string PriceAmount { get; set; }
    [Required] public string CaHash { get; set; }
    public string TransDirect = TransferDirectionType.NFTBuy.ToString();
}

public class CreateNftOrderResponseDto : NftMerchantBaseDto
{
    public string OrderId { get; set; }
}


// query order
public class OrderQueryRequestDto : NftMerchantBaseDto
{
    public string MerchantOrderId { get; set; }
    [Required] public string OrderId { get; set; }
}

public class OrderQueryResponseDto : NftMerchantBaseDto
{
    private string NftSymbol { get; set; }
    private string MerchantOrderId { get; set; }
    private string WebhookUrl { get; set; }
    private string NftPicture { get; set; }
    private string PriceSymbol { get; set; }
    private string PriceAmount { get; set; }
    private string Status { get; set; }
}


// NFT release result
public class NftResultRequestDto : NftMerchantBaseDto
{
    public string ReleaseResult { get; set; }
    public string ReleaseTransactionId { get; set; }
    public string MerchantOrderId { get; set; }
}



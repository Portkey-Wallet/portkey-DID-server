using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

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
    public string OrderId { get; set; }
}

public class OrderQueryResponseDto : NftMerchantBaseDto
{
    public string NftSymbol { get; set; }
    public string MerchantOrderId { get; set; }
    public string WebhookUrl { get; set; }
    public string NftPicture { get; set; }
    public string PriceSymbol { get; set; }
    public string PriceAmount { get; set; }
    public string Status { get; set; }
}


// NFT release result
public class NftResultRequestDto : NftMerchantBaseDto
{
    public string ReleaseResult { get; set; }
    public string ReleaseTransactionId { get; set; }
    public string MerchantOrderId { get; set; }
}


public class NftOrderQueryConditionDto : PagedResultRequestDto
{
    private NftOrderQueryConditionDto() {}

    public NftOrderQueryConditionDto(int skipCount, int maxResultCount)
    {
        base.MaxResultCount = maxResultCount;
        base.SkipCount = skipCount;
    }
    
    public List<Guid> IdIn { get; set; }
    public string MerchantName { get; set; }
    public List<string> MerchantOrderIdIn { get; set; }
}


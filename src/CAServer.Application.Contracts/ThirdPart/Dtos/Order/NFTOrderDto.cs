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
    public string NftCollectionName { get; set; }
    [Required] public string MerchantOrderId { get; set; }
    [Required] public string WebhookUrl { get; set; }
    public string NftPicture { get; set; }
    [Required] public string PaymentSymbol { get; set; }
    [Required] public string PaymentAmount { get; set; }
    [Required] public string CaHash { get; set; }
    [Required] public string MerchantAddress { get; set; }
    [Required] public string UserAddress { get; set; }
    public string TransDirect { get; set; }
}

public class CreateNftOrderResponseDto : NftMerchantBaseDto
{
    public string OrderId { get; set; }
    public string MerchantAddress { get; set; }
}


// query order
public class OrderQueryRequestDto : NftMerchantBaseDto
{
    public string MerchantOrderId { get; set; }
    public string OrderId { get; set; }
}

public class NftOrderQueryResponseDto : NftMerchantBaseDto
{
    public string NftSymbol { get; set; }
    public string NftCollectionName { get; set; }
    public string MerchantOrderId { get; set; }
    public string NftPicture { get; set; }
    public string PaymentSymbol { get; set; }
    public string PaymentAmount { get; set; }
    public string Status { get; set; }
}

// (webhook to Merchant) NFT pay result request dto
public class NftOrderResultRequestDto : NftMerchantBaseDto
{
    public string MerchantOrderId { get; set; }
    public string OrderId { get; set; }
    public string Status { get; set; }
}

// NFT release result
public class NftReleaseResultRequestDto : NftMerchantBaseDto
{
    /// <see cref="NftReleaseResult"/>
    public string ReleaseResult { get; set; }
    public string ReleaseTransactionId { get; set; }
    public string MerchantOrderId { get; set; }
}


public class NftOrderQueryConditionDto : PagedResultRequestDto
{

    public NftOrderQueryConditionDto(int skipCount, int maxResultCount)
    {
        base.MaxResultCount = maxResultCount;
        base.SkipCount = skipCount;
    }
    
    public List<Guid> IdIn { get; set; }
    public string MerchantName { get; set; }
    public List<string> MerchantOrderIdIn { get; set; }
    public string NftSymbol { get; set; }
    
    public string ExpireTimeGt { get; set; }
    public int? WebhookCountGtEq { get; set; }
    public int? WebhookCountLtEq { get; set; }
    public string WebhookStatus { get; set; }
    
    public string WebhookTimeLt { get; set; }
    
    public int? ThirdPartNotifyCountGtEq { get; set; }
    public int? ThirdPartNotifyCountLtEq { get; set; }
    public string ThirdPartNotifyStatus { get; set; }
    
}


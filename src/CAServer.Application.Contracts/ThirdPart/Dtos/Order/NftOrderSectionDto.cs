using System;

namespace CAServer.ThirdPart.Dtos;


public class NftOrderSectionDto : BaseOrderSection
{

    public NftOrderSectionDto() : base(OrderSectionEnum.NftSection)
    {
    }
    
    public Guid Id { get; set; }
    public string NftSymbol { get; set; }
    public string MerchantName { get; set; }
    public string MerchantOrderId { get; set; }
    public string NftPicture { get; set; }
}
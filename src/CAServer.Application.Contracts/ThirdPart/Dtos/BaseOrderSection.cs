using System;

namespace CAServer.ThirdPart.Dtos;

public class BaseOrderSection
{

    public string SectionName { get;}
    
    private BaseOrderSection(){}

    public BaseOrderSection(OrderSectionEnum sectionName)
    {
        SectionName = sectionName.ToString();
    }

}


public enum OrderSectionEnum
{
    NftSection = 0,
}


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
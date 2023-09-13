using System;

namespace CAServer.ThirdPart.Dtos;

public interface IOrderSection{}


public class BaseOrderSection : IOrderSection
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
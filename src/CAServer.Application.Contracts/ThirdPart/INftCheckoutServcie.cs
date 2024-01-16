using CAServer.ThirdPart.Processors;

namespace CAServer.ThirdPart;

public interface INftCheckoutService 
{
    
    IThirdPartNftOrderProcessor GetProcessor(string thirdPartName);
    
}
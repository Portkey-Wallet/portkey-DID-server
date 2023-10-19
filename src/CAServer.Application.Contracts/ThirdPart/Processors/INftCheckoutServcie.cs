using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Processors;

public interface INftCheckoutService 
{
    
    IThirdPartNftOrderProcessor GetProcessor(string thirdPartName);
    
}
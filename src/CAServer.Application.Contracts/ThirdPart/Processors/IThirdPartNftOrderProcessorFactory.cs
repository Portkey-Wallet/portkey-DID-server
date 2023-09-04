using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Processors;

public interface IThirdPartNftOrderProcessorFactory 
{
    
    IThirdPartNftOrderProcessor GetProcessor(string thirdPartName);
    
}
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Processors;

public interface IThirdPartOrderProcessorFactory : ISingletonDependency
{
    
    IThirdPartNftOrderProcessor GetProcessor(string thirdPartName);
    
}
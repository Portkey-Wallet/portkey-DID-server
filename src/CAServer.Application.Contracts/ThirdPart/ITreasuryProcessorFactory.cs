using CAServer.ThirdPart.Processors;

namespace CAServer.ThirdPart;

public interface ITreasuryProcessorFactory
{
    
    public IThirdPartTreasuryProcessor Processor(string thirdPartName);
    
}
namespace CAServer.ThirdPart.Processors;

public interface IThirdPartOrderProcessorFactory
{
    
    IThirdPartOrderProcessor GetProcessor(string thirdPartName);
    
}
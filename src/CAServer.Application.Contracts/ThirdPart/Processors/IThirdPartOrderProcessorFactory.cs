namespace CAServer.ThirdPart.Processors;

public interface IThirdPartOrderProcessorFactory
{
    
    IThirdPartNftOrderProcessor GetProcessor(string thirdPartName);
    
}
namespace CAServer.ThirdPart;

public interface IOrderProcessorFactory
{
    public IOrderProcessor GetProcessor(string merchantName);
    
}
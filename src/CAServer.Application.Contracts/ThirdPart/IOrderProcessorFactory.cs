namespace CAServer.ThirdPart;

public interface IOrderProcessorFactory
{
    public IOrderProcessor GetProcessor(string merchantName);

    public IThirdPartAppService GetAppService(string merchantName);

}
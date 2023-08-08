namespace CAServer.ThirdPart;

public interface IThirdPartFactory
{
    public IOrderProcessor GetProcessor(string merchantName);

    public IThirdPartAppService GetAppService(string merchantName);

}
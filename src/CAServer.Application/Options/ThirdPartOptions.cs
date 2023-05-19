namespace CAServer.Options;

public class ThirdPartOptions
{
    public AlchemyOptions alchemy { get; set; }
    public BridgeOptions bridge { get; set; }
}

public class BridgeOptions
{
    public int Delay { get; set; }
    public int Timeout { get; set; }
}

public class AlchemyOptions
{
    public string AppId { get; set; }
    public string AppSecret { get; set; }
    public string BaseUrl { get; set; }
}




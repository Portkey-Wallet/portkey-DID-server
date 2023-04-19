namespace CAServer.Options;

public class ThirdPartOptions
{
    public AlchemyOptions alchemy { get; set; }
}
public class AlchemyOptions
{
    public string AppId { get; set; }
    public string AppSecret { get; set; }
    public string BaseUrl { get; set; }
}


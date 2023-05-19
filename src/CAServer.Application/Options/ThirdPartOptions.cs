namespace CAServer.Options;

public class ThirdPartOptions
{
    public AlchemyOptions alchemy { get; set; }
    public ThirdPartTimerOptions timer { get; set; }
}

public class ThirdPartTimerOptions
{
    public int Delay { get; set; } = 1;
    public int Timeout { get; set; } = 5;
}

public class AlchemyOptions
{
    public string AppId { get; set; }
    public string AppSecret { get; set; }
    public string BaseUrl { get; set; }
}
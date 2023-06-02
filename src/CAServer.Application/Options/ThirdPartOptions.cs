namespace CAServer.Options;

public class ThirdPartOptions
{
    public AlchemyOptions alchemy { get; set; }
    public ThirdPartTimerOptions timer { get; set; }
}

public class ThirdPartTimerOptions
{
    public int DelaySeconds { get; set; } = 1;
    public int TimeoutMillis { get; set; } = 5000;
}

public class AlchemyOptions
{
    public string AppId { get; set; }
    public string AppSecret { get; set; }
    public string BaseUrl { get; set; }
    public string UpdateSellOrderUri { get; set; }
    public string FiatListUri { get; set; }
    public string CryptoListUri { get; set; }
    public string OrderQuoteUri { get; set; }
    public string GetTokenUri { get; set; }
    public bool SkipCheckSign { get; set; } = false;
}
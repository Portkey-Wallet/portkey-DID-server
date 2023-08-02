using CAServer.Commons;

namespace CAServer.Options;

public class ThirdPartOptions
{
    public AlchemyOptions alchemy { get; set; }
    public TransakOptions transak { get; set; }
    public ThirdPartTimerOptions timer { get; set; } = new ThirdPartTimerOptions();
}

public class ThirdPartTimerOptions
{
    public int DelaySeconds { get; set; } = 1;
    public int TimeoutMillis { get; set; } = 60000;
    
    public int HandleUnCompletedOrderMinuteAgo { get; set; } = 2;
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
    public int FiatListExpirationMinutes { get; set; } = CommonConstant.FiatListExpirationMinutes;
    public int OrderQuoteExpirationMinutes { get; set; } = CommonConstant.OrderQuoteExpirationMinutes;
    public string MerchantQueryTradeUri { get; set; }
}

public class TransakOptions
{
    public string AppId { get; set; }
    public string AppSecret { get; set; }
    public string BaseUrl { get; set; }
}
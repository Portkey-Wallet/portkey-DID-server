namespace CAServer.Options;

public class ExchangeApiOptions
{
    public ExchangeApiBaseOption Mastercard { get; set; }
    public ExchangeApiBaseOption ApiLayerExchange { get; set; }
    
}

public class ExchangeApiBaseOption
{
    public string BaseUrl { get; set; }
    public string AppId { get; set; }
    public string AppSecret { get; set; }
}
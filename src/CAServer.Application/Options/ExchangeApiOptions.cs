namespace CAServer.Options;

public class ExchangeApiOptions
{
    public MastercardOption Mastercard { get; set; }
}

public class MastercardOption
{
    public string BaseUrl { get; set; }
}
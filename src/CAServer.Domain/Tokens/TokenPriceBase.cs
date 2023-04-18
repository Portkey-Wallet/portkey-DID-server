namespace CAServer.Tokens;

public class TokenPriceBase
{
    public string Id { get; set; }
    public string Symbol {get; set; }
    public decimal PriceInUsd {get; set; }

}
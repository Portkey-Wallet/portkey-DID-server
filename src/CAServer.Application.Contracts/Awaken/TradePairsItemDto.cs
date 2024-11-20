namespace CAServer.Awaken;

public class TradePairsItemDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string Address { get; set; }
    public TradePairsItemToken Token0 { get; set; }
    public TradePairsItemToken Token1 { get; set; }
}

public class TradePairsItemToken
{
    public string Id;
    public string Address;
    public string Symbol;
    public int Decimals;
    public string ImageUri;
    public string ChainId;
}
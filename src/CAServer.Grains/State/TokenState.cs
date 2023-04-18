namespace CAServer.Grains.State;

public class TokenState
{
    public Guid Id { get; set; }
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string Symbol { get; set; }

    public int Decimal { get; set; }

    public decimal PriceInUsd { get; set; }

    public DateTime PriceUpdateTime { get; set; }
}
namespace CAServer.Grains.Grain.ZeroHoldings;

[GenerateSerializer]
public class ZeroHoldingsGrainDto
{
    [Id(0)]
    public Guid UserId { get; set; }
    [Id(1)]
    public string Status { get; set; }
}
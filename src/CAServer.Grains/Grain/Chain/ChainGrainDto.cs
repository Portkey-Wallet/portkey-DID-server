using CAServer.Chain;

namespace CAServer.Grains.Grain.Account;

[GenerateSerializer]
public class ChainGrainDto : ChainDto
{
    [Id(0)]
    public string Id { get; set; }
}
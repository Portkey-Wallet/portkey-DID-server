using CAServer.Chain;

namespace CAServer.Grains.Grain.Account;

[GenerateSerializer]
public class ChainGrainDto : ChainDto
{
    public string Id { get; set; }
}
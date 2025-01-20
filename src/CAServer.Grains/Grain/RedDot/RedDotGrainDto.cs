using CAServer.RedDot.Dtos;

namespace CAServer.Grains.Grain.RedDot;

[GenerateSerializer]
public class RedDotGrainDto
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public List<RedDotInfo> RedDotInfos { get; set; } = new();
}
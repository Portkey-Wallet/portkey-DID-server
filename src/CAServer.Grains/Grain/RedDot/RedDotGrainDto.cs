using CAServer.RedDot.Dtos;

namespace CAServer.Grains.Grain.RedDot;

public class RedDotGrainDto
{
    public string Id { get; set; }
    public List<RedDotInfo> RedDotInfos { get; set; } = new();
}
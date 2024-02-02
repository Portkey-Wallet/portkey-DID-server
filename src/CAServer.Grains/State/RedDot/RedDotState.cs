using CAServer.RedDot.Dtos;

namespace CAServer.Grains.State.RedDot;

public class RedDotState
{
    public string Id { get; set; }
    public List<RedDotInfo> RedDotInfos { get; set; } = new();
}
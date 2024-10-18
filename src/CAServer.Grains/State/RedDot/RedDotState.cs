using CAServer.RedDot.Dtos;

namespace CAServer.Grains.State.RedDot;

[GenerateSerializer]
public class RedDotState
{
	[Id(0)]
    public string Id { get; set; }
	[Id(1)]
    public List<RedDotInfo> RedDotInfos { get; set; } = new();
}

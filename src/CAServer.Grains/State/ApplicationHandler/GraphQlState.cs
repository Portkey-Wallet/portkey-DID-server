namespace CAServer.Grains.State.ApplicationHandler;

[GenerateSerializer]
public class GraphQlState
{
	[Id(0)]
    public long EndHeight { get; set; }
}

namespace CAServer.Grains.State;

[GenerateSerializer]
public class SampleState
{
    [Id(0)]
    public string From { get; set; }
    [Id(1)]
    public string To { get; set; }
    [Id(2)]
    public string Message { get; set; }
}
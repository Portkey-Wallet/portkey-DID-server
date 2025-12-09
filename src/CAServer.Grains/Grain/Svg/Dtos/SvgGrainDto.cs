namespace CAServer.Grains.Grain.Svg.Dtos;

[GenerateSerializer]
public class SvgGrainDto
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public string AmazonUrl { get; set; }

    [Id(2)]
    public string Svg { get; set; }
}
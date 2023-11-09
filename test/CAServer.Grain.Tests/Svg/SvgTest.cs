using CAServer.Grains.Grain.Svg;
using CAServer.Grains.Grain.Svg.Dtos;
using Xunit;

namespace CAServer.Grain.Tests.Svg;

public class SvgTest : CAServerGrainTestBase
{
    [Fact]
    public async void GrainSvgAddTest()
    {
        var grain = Cluster.Client.GetGrain<ISvgGrain>(Guid.NewGuid().ToString());
        SvgGrainDto svgGrainDto = new SvgGrainDto();
        svgGrainDto.AmazonUrl = "";
        var res = await grain.GetSvgAsync();
        Console.WriteLine("123");

    }
}
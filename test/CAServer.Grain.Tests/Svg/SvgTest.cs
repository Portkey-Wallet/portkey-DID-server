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
        svgGrainDto.Id = new Guid();
        svgGrainDto.Name = "USA";
        svgGrainDto.Md5 = "test MD5";
        svgGrainDto.SvgContent = "svgxml";
        svgGrainDto.AmazonUrl = "";
        var result = await grain.AddSvgAsync(svgGrainDto);
    }
}
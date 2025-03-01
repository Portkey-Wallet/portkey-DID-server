using CAServer.Grains.Grain.Svg.Dtos;

namespace CAServer.Grains.Grain.Svg;

public interface ISvgGrain : IGrainWithStringKey
{
    Task<GrainResultDto<SvgGrainDto>> AddSvgAsync(SvgGrainDto svgGrainDto);

    Task<SvgGrainDto> GetSvgAsync();

}
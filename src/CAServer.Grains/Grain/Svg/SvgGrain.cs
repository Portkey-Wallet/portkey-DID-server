using AutoMapper.Internal.Mappers;
using CAServer.Grains.Grain.Svg.Dtos;
using CAServer.Grains.State.SvgPart;
using Microsoft.Extensions.Logging;

namespace CAServer.Grains.Grain.Svg;

public class SvgGrain :Orleans.Grain<SvgState> , ISvgGrain
{
    private readonly ILogger<SvgGrain> _logger;
    public SvgGrain(ILogger<SvgGrain> logger)
    {
        _logger = logger;
    }
    public async Task<GrainResultDto<SvgGrainDto>> AddSvgAsync(SvgGrainDto svgGrainDto)
    {
        //TODO make some check include (MD5, filename)
        //amazone can be null
        
        var result = new GrainResultDto<SvgGrainDto>();
        State.Id = svgGrainDto.Md5;
        State.Md5 = svgGrainDto.Md5;
        await WriteStateAsync();
        result.Success = true;
        result.Data = svgGrainDto;
      

        return result;
    }
}
using CAServer.Grains.Grain.Svg.Dtos;
using CAServer.Grains.State.SvgPart;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Svg;

public class SvgGrain :Orleans.Grain<SvgState> , ISvgGrain
{
    private readonly ILogger<SvgGrain> _logger;
    private readonly IObjectMapper _objectMapper;

    public SvgGrain(ILogger<SvgGrain> logger,IObjectMapper objectMapper)
    {
        _logger = logger;
        _objectMapper = objectMapper;
    }
    public async Task<GrainResultDto<SvgGrainDto>> AddSvgAsync(SvgGrainDto svgGrainDto)
    {
        //TODO make some check include (MD5, filename)
        //amazone can be null
        
        var result = new GrainResultDto<SvgGrainDto>();
        State.Svg = svgGrainDto.Svg;
        State.AmazonUrl = svgGrainDto.AmazonUrl;
        State.Id = svgGrainDto.Id;
        await WriteStateAsync();
        result.Success = true;
        result.Data = svgGrainDto;
      

        return result;
    }

    public Task<SvgGrainDto> GetSvgAsync()
    {
        return  Task.FromResult(_objectMapper.Map<SvgState, SvgGrainDto>(State));
        
    }
}
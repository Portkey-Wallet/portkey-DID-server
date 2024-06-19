using CAServer.EnumType;
using CAServer.FreeMint.Dtos;
using CAServer.Grains.State.FreeMint;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.FreeMint;

public class FreeMintGrain : Grain<FreeMintState>, IFreeMintGrain
{
    private readonly IObjectMapper _objectMapper;

    public FreeMintGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public Task<GrainResultDto<FreeMintGrainDto>> GetFreeMintInfo()
    {
        var result = new GrainResultDto<FreeMintGrainDto>();
        if (string.IsNullOrEmpty(State.Id))
        {
            result.Message = "Free mint info not exist.";
            return Task.FromResult(result);
        }

        result.Success = true;
        result.Data = _objectMapper.Map<FreeMintState, FreeMintGrainDto>(State);
        return Task.FromResult(result);
    }

    public Task<GrainResultDto<GetRecentStatusDto>> GetRecentStatus()
    {
        var result = new GrainResultDto<GetRecentStatusDto>();
        var statusDto = new GetRecentStatusDto
        {
            Status = FreeMintStatus.NONE
        };
        
        if (!string.IsNullOrEmpty(State.Id) && !State.MintInfos.IsNullOrEmpty())
        {
            statusDto.Status = State.MintInfos.Last().Status;
            statusDto.ItemId = State.MintInfos.Last().ItemId;
        }

        result.Success = true;
        result.Data = statusDto;
        return Task.FromResult(result);
    }

    public Task<GrainResultDto<GetRecentStatusDto>> Mint()
    {
        throw new NotImplementedException();
    }

    public Task<GrainResultDto<GetRecentStatusDto>> Mint(ItemMintInfo itemMintInfo)
    {
        var result = new GrainResultDto<GetRecentStatusDto>();
        
        
        result.Success = true;
        //result.Data = _objectMapper.Map<FreeMintState, FreeMintGrainDto>(State);
        return Task.FromResult(result);
    }
}
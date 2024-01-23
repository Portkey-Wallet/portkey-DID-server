using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.State.Order;
using CAServer.ThirdPart;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.ThirdPart;

public interface ITreasuryOrderGrain : IGrainWithGuidKey
{
    
    Task<GrainResultDto<TreasuryOrderDto>> SaveOrUpdateAsync(TreasuryOrderDto orderDto);
    
    Task<GrainResultDto<TreasuryOrderDto>> GetAsync();
    
}

public class TreasuryOrderGrain : Grain<TreasuryOrderState>, ITreasuryOrderGrain
{
    private readonly IObjectMapper _objectMapper;

    public TreasuryOrderGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }


    public async Task<GrainResultDto<TreasuryOrderDto>> SaveOrUpdateAsync(TreasuryOrderDto orderDto)
    {
        var createTime = State.CreateTime;
        _objectMapper.Map(orderDto, State);

        if (orderDto.Version != State.Version)
        {
            return new GrainResultDto<TreasuryOrderDto>().Error("Data expired, current version is " + State.Version);
        }

        State.Id = this.GetPrimaryKey();
        State.CreateTime = createTime == 0 ? DateTime.UtcNow.ToUtcMilliSeconds() : createTime;
        State.LastModifyTime = DateTime.UtcNow.ToUtcMilliSeconds();
        State.Version = State.Version ++;

        await WriteStateAsync();

        return new GrainResultDto<TreasuryOrderDto>();
    }


    public Task<GrainResultDto<TreasuryOrderDto>> GetAsync()
    {
        return Task.FromResult(
            new GrainResultDto<TreasuryOrderDto>(_objectMapper.Map<TreasuryOrderState, TreasuryOrderDto>(State)));
    }
}
using CAServer.Grains.State.Order;
using CAServer.ThirdPart;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.ThirdPart;

public class OrderSettlementGrain : Grain<OrderSettlementState>, IOrderSettlementGrain
{
    private readonly IObjectMapper _objectMapper;

    public OrderSettlementGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    
    public async Task<GrainResultDto<OrderSettlementGrainDto>> AddUpdate(OrderSettlementGrainDto grainDto)
    {
        _objectMapper.Map(grainDto, State);

        State.Id = State.Id == Guid.Empty ? this.GetPrimaryKey() : State.Id;

        await WriteStateAsync();

        return new GrainResultDto<OrderSettlementGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<OrderSettlementState, OrderSettlementGrainDto>(State)
        };
    }

    public Task<GrainResultDto<OrderSettlementGrainDto>> GetById(Guid id)
    {
        return Task.FromResult(new GrainResultDto<OrderSettlementGrainDto>
        {
            Success = true,
            Data = State.Id == Guid.Empty
                ? null
                : _objectMapper.Map<OrderSettlementState, OrderSettlementGrainDto>(State)
        });
    }
}
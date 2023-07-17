using CAServer.Commons;
using CAServer.Grains.State.Order;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.ThirdPart;

public class OrderGrain : Grain<OrderState>, IOrderGrain
{
    private readonly IObjectMapper _objectMapper;

    public OrderGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<GrainResultDto<OrderGrainDto>> CreateUserOrderAsync(OrderGrainDto input)
    {
        State = _objectMapper.Map<OrderGrainDto, OrderState>(input);
        if (State.Id == Guid.Empty)
        {
            State.Id = this.GetPrimaryKey();
        }

        await WriteStateAsync();

        return new GrainResultDto<OrderGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<OrderState, OrderGrainDto>(State)
        };
    }


    public async Task<GrainResultDto<OrderGrainDto>> UpdateOrderAsync(OrderGrainDto input)
    {
        var result = new GrainResultDto<OrderGrainDto>();

        State = _objectMapper.Map<OrderGrainDto, OrderState>(input);
        if (State.Id == Guid.Empty)
        {
            State.Id = this.GetPrimaryKey();
        }

        await WriteStateAsync();

        result.Data = _objectMapper.Map<OrderState, OrderGrainDto>(State);
        result.Success = true;
        return result;
    }

    private IOrderStatusInfoGrain GetOrderStatusInfoGrain(Guid id)
    {
        return GrainFactory.GetGrain<IOrderStatusInfoGrain>(
            GrainIdHelper.GenerateGrainId(CommonConstant.OrderStatusInfoPrefix, id.ToString("N")));
    }
}
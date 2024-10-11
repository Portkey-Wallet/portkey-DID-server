using CAServer.Grains.State.Order;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.ThirdPart;

public class OrderStatusInfoGrain : Grain<OrderStatusInfoState>, IOrderStatusInfoGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<OrderStatusInfoGrain> _logger;

    public OrderStatusInfoGrain(IObjectMapper objectMapper, ILogger<OrderStatusInfoGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, token);
    }

    public async Task<OrderStatusInfoGrainResultDto> AddOrderStatusInfo(OrderStatusInfoGrainDto grainDto)
    {
        if (string.IsNullOrWhiteSpace(State.Id))
        {
            State = _objectMapper.Map<OrderStatusInfoGrainDto, OrderStatusInfoState>(grainDto);
            State.Id = this.GetPrimaryKeyString();
        }

        if (string.IsNullOrWhiteSpace(State.RawTransaction) && !string.IsNullOrWhiteSpace(grainDto.RawTransaction))
        {
            State.RawTransaction = grainDto.RawTransaction;
        }

        State.ThirdPartOrderNo = grainDto.ThirdPartOrderNo;
        State.OrderStatusList.Add(grainDto.OrderStatusInfo);
        await WriteStateAsync();

        return _objectMapper.Map<OrderStatusInfoState, OrderStatusInfoGrainResultDto>(State);
    }
}
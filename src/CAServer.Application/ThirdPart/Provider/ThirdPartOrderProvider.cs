using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using Microsoft.Extensions.Options;
using Nest;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using OrderStatusInfo = CAServer.ThirdPart.Dtos.OrderStatusInfo;

namespace CAServer.ThirdPart.Provider;

public class ThirdPartOrderProvider : IThirdPartOrderProvider, ISingletonDependency
{
    private readonly INESTRepository<RampOrderIndex, Guid> _orderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ThirdPartOptions _thirdPartOptions;

    public ThirdPartOrderProvider(INESTRepository<RampOrderIndex, Guid> orderRepository,
        IObjectMapper objectMapper,
        IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus, IOptions<ThirdPartOptions> thirdPartOptions)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _thirdPartOptions = thirdPartOptions.Value;
    }

    public async Task<RampOrderIndex> GetThirdPartOrderIndexAsync(string orderId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RampOrderIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(orderId)));

        QueryContainer Filter(QueryContainerDescriptor<RampOrderIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, userOrders) = await _orderRepository.GetListAsync(Filter);

        return totalCount < 1 ? null : userOrders.First();
    }

    public async Task<OrderDto> GetThirdPartOrderAsync(string orderId)
    {
        var orderIndex = await GetThirdPartOrderIndexAsync(orderId);
        return orderIndex == null ? new OrderDto() : _objectMapper.Map<RampOrderIndex, OrderDto>(orderIndex);
    }

    public async Task<List<OrderDto>> GetUnCompletedThirdPartOrdersAsync()
    {
        const string transDirectSell = "TokenSell";
        var modifyTimeLt = DateTimeOffset.UtcNow.AddMinutes(_thirdPartOptions.timer.HandleUnCompletedOrderMinuteAgo)
            .ToUnixTimeMilliseconds();
        var mustQuery = new List<Func<QueryContainerDescriptor<RampOrderIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Status).Terms(OrderStatusType.Created.ToString())));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.IsDeleted).Terms(false)));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.TransDirect).Terms(transDirectSell)));
        mustQuery.Add(q => q.TermRange(i => i.Field(f => f.LastModifyTime).LessThan(modifyTimeLt.ToString())));

        QueryContainer Filter(QueryContainerDescriptor<RampOrderIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, userOrders) = await _orderRepository.GetListAsync(Filter);

        if (totalCount < 1)
        {
            return new List<OrderDto>();
        }

        return _objectMapper.Map<List<RampOrderIndex>, List<OrderDto>>(userOrders);
    }

    public async Task<List<OrderDto>> GetThirdPartOrdersByPageAsync(Guid userId, int skipCount, int maxResultCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RampOrderIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.UserId).Terms(userId)));

        QueryContainer Filter(QueryContainerDescriptor<RampOrderIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        IPromise<IList<ISort>> Sort(SortDescriptor<RampOrderIndex> s) => s.Descending(a => a.LastModifyTime);

        var (totalCount, userOrders) =
            await _orderRepository.GetSortListAsync(Filter, sortFunc: Sort, limit: maxResultCount, skip: skipCount);

        if (totalCount < 1)
        {
            return new List<OrderDto>();
        }

        return userOrders.Select(i => _objectMapper.Map<RampOrderIndex, OrderDto>(i)).ToList();
    }

    public async Task AddOrderStatusInfoAsync(OrderStatusInfoGrainDto grainDto)
    {
        var orderStatusGrain = _clusterClient.GetGrain<IOrderStatusInfoGrain>(
            GrainIdHelper.GenerateGrainId(CommonConstant.OrderStatusInfoPrefix, grainDto.OrderId.ToString("N")));
        var addResult = await orderStatusGrain.AddOrderStatusInfo(grainDto);
        if (addResult == null) return;

        var eto = _objectMapper.Map<OrderStatusInfoGrainResultDto, OrderStatusInfoEto>(addResult);
        await _distributedEventBus.PublishAsync(eto);
    }

    public async Task AddOrderStatusInfoAsync(string orderId, OrderStatusInfo orderStatusInfo)
    {
        var order = await GetThirdPartOrderAsync(orderId);
        if (order == null || order.Id == Guid.Empty) return;

        var grainDto = _objectMapper.Map<OrderDto, OrderStatusInfoGrainDto>(order);
        grainDto.OrderStatusInfo = orderStatusInfo;
        await AddOrderStatusInfoAsync(grainDto);
    }
}
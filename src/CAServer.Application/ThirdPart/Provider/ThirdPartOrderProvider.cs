using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos;
using Nest;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Provider;

public class ThirdPartOrderProvider : IThirdPartOrderProvider, ISingletonDependency
{
    private readonly INESTRepository<OrderIndex, Guid> _orderRepository;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;

    public ThirdPartOrderProvider(INESTRepository<OrderIndex, Guid> orderRepository,
        IObjectMapper objectMapper, IClusterClient clusterClient)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
    }

    public async Task<OrderDto> GetThirdPartOrderAsync(string orderId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OrderIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(orderId)));

        QueryContainer Filter(QueryContainerDescriptor<OrderIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, userOrders) = await _orderRepository.GetListAsync(Filter);
        if (totalCount < 1)
        {
            return new OrderDto();
        }

        return _objectMapper.Map<OrderIndex, OrderDto>(userOrders.First());
    }

    public async Task<OrderDto> GetThirdPartOrderFromGrainAsync(Guid orderId)
    {
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);
        var order = await orderGrain.GetOrder();
        return order.Success ? _objectMapper.Map<OrderGrainDto, OrderDto>(order.Data) : null;
    }

    public async Task<List<OrderDto>> GetThirdPartOrdersByPageAsync(Guid userId, int skipCount, int maxResultCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OrderIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.UserId).Terms(userId)));

        QueryContainer Filter(QueryContainerDescriptor<OrderIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        IPromise<IList<ISort>> Sort(SortDescriptor<OrderIndex> s) => s.Descending(a => a.LastModifyTime);

        var (totalCount, userOrders) =
            await _orderRepository.GetSortListAsync(Filter, sortFunc: Sort, limit: maxResultCount, skip: skipCount);

        if (totalCount < 1)
        {
            return new List<OrderDto>();
        }

        return userOrders.Select(i => _objectMapper.Map<OrderIndex, OrderDto>(i)).ToList();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.ThirdPart.Dtos;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Provider;

public class ThirdPartOrderProvider : IThirdPartOrderProvider, ISingletonDependency
{
    private readonly INESTRepository<OrderIndex, Guid> _orderRepository;
    private readonly IObjectMapper _objectMapper;

    public ThirdPartOrderProvider(INESTRepository<OrderIndex, Guid> orderRepository,
        IObjectMapper objectMapper)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
    }

    public async Task<OrderIndex> GetThirdPartOrderIndexAsync(string orderId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OrderIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(orderId)));

        QueryContainer Filter(QueryContainerDescriptor<OrderIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, userOrders) = await _orderRepository.GetListAsync(Filter);
        
        return totalCount < 1 ? null : userOrders.First();
    }
    
    public async Task<OrderDto> GetThirdPartOrderAsync(string orderId)
    {
        var orderIndex = await GetThirdPartOrderIndexAsync(orderId);
        return orderIndex == null ? new OrderDto() : _objectMapper.Map<OrderIndex, OrderDto>(orderIndex);
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
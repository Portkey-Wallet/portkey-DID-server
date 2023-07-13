using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using MockServer.Dtos;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace MockServer.Controllers;

public class AlchemyMockServerProvider : IAlchemyMockServerProvider, ISingletonDependency
{
    private readonly INESTRepository<AlchemyOrderIndex, Guid> _orderRepository;
    private readonly IObjectMapper _objectMapper;

    public AlchemyMockServerProvider(INESTRepository<AlchemyOrderIndex, Guid> orderRepository,
        IObjectMapper objectMapper)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
    }

    public async Task<AlchemyOrderDto> GetThirdPartOrderAsync(string orderNo)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<AlchemyOrderIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.OrderNo).Terms(orderNo)));

        QueryContainer Filter(QueryContainerDescriptor<AlchemyOrderIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, userOrders) = await _orderRepository.GetListAsync(Filter);
        if (totalCount < 1)
        {
            return new AlchemyOrderDto();
        }

        return _objectMapper.Map<AlchemyOrderIndex, AlchemyOrderDto>(userOrders.First());
    }
}

public interface IAlchemyMockServerProvider
{
    public Task<AlchemyOrderDto> GetThirdPartOrderAsync(string orderNo);
}
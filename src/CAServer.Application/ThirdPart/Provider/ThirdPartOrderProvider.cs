using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.LinqToElasticSearch.Provider;
using CAServer.Entities.Es;
using CAServer.ThirdPart.Dtos;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Provider;

public class ThirdPartOrderProvider : IThirdPartOrderProvider, ISingletonDependency
{
    private readonly ILinqRepository<OrderIndex, Guid> _orderRepository;
    private readonly IObjectMapper _objectMapper;

    public ThirdPartOrderProvider(ILinqRepository<OrderIndex, Guid> orderRepository,
        IObjectMapper objectMapper)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
    }

    public async Task<OrderDto> GetThirdPartOrderAsync(string orderId)
    {
        Expression<Func<OrderIndex, bool>> expression = f=> f.Id ==Guid.Parse(orderId) ;

        var userOrders = _orderRepository.WhereClause(expression).ToList();
        if (userOrders.Count < 1)
        {
            return new OrderDto();
        }

        return _objectMapper.Map<OrderIndex, OrderDto>(userOrders.First());
    }

    public async Task<List<OrderDto>> GetThirdPartOrdersByPageAsync(Guid userId, int skipCount, int maxResultCount)
    {
        Expression<Func<OrderIndex, bool>> expression = f=> f.UserId == userId ;

        var userOrders =
            _orderRepository.WhereClause(expression).OrderDesc(f => f.LastModifyTime).ToList();

        if (userOrders.Count() < 1)
        {
            return new List<OrderDto>();
        }

        return userOrders.Select(i => _objectMapper.Map<OrderIndex, OrderDto>(i)).ToList();
    }
}
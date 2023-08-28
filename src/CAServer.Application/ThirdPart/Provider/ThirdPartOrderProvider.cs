using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Commons.Dtos;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using Elasticsearch.Net;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Org.BouncyCastle.Crypto.Digests;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Provider;

public class ThirdPartOrderProvider : IThirdPartOrderProvider, ISingletonDependency
{
    private static readonly List<string> NftTransDirect = new()
    {
        TransferDirectionType.NFTBuy.ToString(),
        TransferDirectionType.NFTSell.ToString()
    };


    private readonly INESTRepository<RampOrderIndex, Guid> _orderRepository;
    private readonly INESTRepository<NftOrderIndex, Guid> _nftOrderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ThirdPartOptions _thirdPartOptions;

    public ThirdPartOrderProvider(INESTRepository<RampOrderIndex, Guid> orderRepository,
        IObjectMapper objectMapper,
        IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus, IOptions<ThirdPartOptions> thirdPartOptions,
        INESTRepository<NftOrderIndex, Guid> nftOrderRepository)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _nftOrderRepository = nftOrderRepository;
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
        var modifyTimeLt = DateTimeOffset.UtcNow.AddMinutes(_thirdPartOptions.Timer.HandleUnCompletedOrderMinuteAgo)
            .ToUnixTimeMilliseconds();
        var unCompletedState = new List<string>
        {
            OrderStatusType.Transferred.ToString(),
            OrderStatusType.UserCompletesCoinDeposit.ToString(),
            OrderStatusType.StartPayment.ToString(),
        };
        var mustQuery = new List<Func<QueryContainerDescriptor<RampOrderIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Status).Terms(unCompletedState)));
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

    public async Task<PageResultDto<OrderDto>> GetThirdPartOrdersByPageAsync(Guid userId, List<Guid> orderIdIn,
        int skipCount, int maxResultCount, params OrderSectionEnum?[] withSections)
    {
        AssertHelper.IsTrue(userId != Guid.Empty || !orderIdIn.IsNullOrEmpty(), "userId or orderId required.");
        var mustQuery = new List<Func<QueryContainerDescriptor<RampOrderIndex>, QueryContainer>>() { };
        if (userId != Guid.Empty)
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.UserId).Terms(userId)));
        }

        if (!orderIdIn.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(orderIdIn)));
        }

        QueryContainer Filter(QueryContainerDescriptor<RampOrderIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        IPromise<IList<ISort>> Sort(SortDescriptor<RampOrderIndex> s) => s.Descending(a => a.LastModifyTime);

        var (totalCount, userOrders) =
            await _orderRepository.GetSortListAsync(Filter, sortFunc: Sort, limit: maxResultCount, skip: skipCount);

        var pager = new PageResultDto<OrderDto>(
            userOrders.Select(i => _objectMapper.Map<RampOrderIndex, OrderDto>(i)).ToList(), totalCount);

        if (withSections.Contains(OrderSectionEnum.NftSection))
        {
            var nftOrderPager = await QueryNftOrderPagerAsync(new NftOrderQueryConditionDto(0, maxResultCount)
            {
                IdIn = pager.Data.Where(order => NftTransDirect.Contains(order.TransDirect)).Select(order => order.Id).ToList()
            });
            FillNftOrderSection(pager, nftOrderPager);
        }

        return pager;
    }

    public async Task<PageResultDto<OrderDto>> GetNftOrdersByPageAsync(NftOrderQueryConditionDto condition)
    {
        var nftOrderPager = await QueryNftOrderPagerAsync(condition);
        if (nftOrderPager.Data.IsNullOrEmpty()) return new PageResultDto<OrderDto>();
        var orderIds = nftOrderPager.Data.Select(order => order.Id).ToList();
        var orderPager = await GetThirdPartOrdersByPageAsync(Guid.Empty, orderIds, 0, orderIds.Count);
        FillNftOrderSection(orderPager, nftOrderPager);
        return orderPager;
    }
    
    private async Task<PageResultDto<NftOrderIndex>> QueryNftOrderPagerAsync(NftOrderQueryConditionDto condition)
    {
        AssertHelper.IsTrue(!condition.IdIn.IsNullOrEmpty() || !condition.MerchantOrderIdIn.IsNullOrEmpty(),
            "IdIn or MerchantOrderIdIn required.");
        AssertHelper.IsTrue(condition.MerchantOrderIdIn.IsNullOrEmpty() || !condition.MerchantName.IsNullOrEmpty(),
            "MerchantName required when MerchantOrderIdIn not empty");

        var mustQuery = new List<Func<QueryContainerDescriptor<NftOrderIndex>, QueryContainer>> { };

        // by id
        if (!condition.IdIn.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(condition.IdIn)));
        }

        // by merchantOrderId
        if (!condition.MerchantOrderIdIn.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.MerchantName).Terms(condition.MerchantName)));
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.MerchantOrderId).Terms(condition.MerchantOrderIdIn)));
        }

        IPromise<IList<ISort>> Sort(SortDescriptor<NftOrderIndex> s) => s.Descending(a => a.Id);
        QueryContainer Filter(QueryContainerDescriptor<NftOrderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, nftOrders) = await _nftOrderRepository.GetSortListAsync(Filter, sortFunc: Sort,
            limit: condition.MaxResultCount, skip: condition.SkipCount);
        return new PageResultDto<NftOrderIndex>(nftOrders, totalCount);
    }

    private void FillNftOrderSection(PageResultDto<OrderDto> orderPager, PageResultDto<NftOrderIndex> nftOrderPager)
    {
        if (nftOrderPager.Data.IsNullOrEmpty()) return;
        var nftOrderIndices = nftOrderPager.Data.ToDictionary(order => order.Id, order => order);
        foreach (var orderDto in orderPager.Data)
        {
            if (nftOrderIndices.ContainsKey(orderDto.Id)) continue;
            var nftOrderIndex = nftOrderIndices[orderDto.Id];
            var nftOrderSection = _objectMapper.Map<NftOrderIndex, NftOrderSectionDto>(nftOrderIndex);
            orderDto.OrderSections.Add(nftOrderSection.SectionName, nftOrderSection);
        }
    }
}
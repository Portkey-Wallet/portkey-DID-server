using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Etos;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using OrderStatusInfo = CAServer.ThirdPart.Dtos.OrderStatusInfo;

namespace CAServer.ThirdPart.Provider;

public class TreasuryOrderProvider : ITreasuryOrderProvider
{
    private readonly ILogger<TreasuryOrderProvider> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<TreasuryOrderIndex, Guid> _treasuryOrderRepository;
    private readonly INESTRepository<PendingTreasuryOrderIndex, Guid> _pendingTreasuryOrderRepository;
    private readonly INESTRepository<OrderStatusInfoIndex, string> _orderStatusInfoRepository;
    private readonly IOrderStatusProvider _orderStatusProvider;

    public TreasuryOrderProvider(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        INESTRepository<TreasuryOrderIndex, Guid> treasuryOrderRepository, IObjectMapper objectMapper,
        IOrderStatusProvider orderStatusProvider, ILogger<TreasuryOrderProvider> logger,
        INESTRepository<PendingTreasuryOrderIndex, Guid> pendingTreasuryOrderRepository,
        INESTRepository<OrderStatusInfoIndex, string> orderStatusInfoRepository)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _treasuryOrderRepository = treasuryOrderRepository;
        _objectMapper = objectMapper;
        _orderStatusProvider = orderStatusProvider;
        _logger = logger;
        _pendingTreasuryOrderRepository = pendingTreasuryOrderRepository;
        _orderStatusInfoRepository = orderStatusInfoRepository;
    }


    public async Task<TreasuryOrderDto> DoSaveOrderAsync(TreasuryOrderDto orderDto,
        Dictionary<string, string> externalData = null)
    {
        var treasuryOrderGrain = _clusterClient.GetGrain<ITreasuryOrderGrain>(orderDto.Id);
        var oldOrder = await treasuryOrderGrain.GetAsync();
        AssertHelper.IsTrue(oldOrder.Success, "old order not exists");
        AssertHelper.IsTrue(oldOrder.Data.Version <= orderDto.Version,
            "order expired Id={}, version={}, inputVersion={}", orderDto.Id, oldOrder.Data.Version, orderDto.Version);
        AssertHelper.IsTrue(oldOrder.Data.ToAddress.IsNullOrEmpty() || oldOrder.Data.ToAddress == orderDto.ToAddress,
            "To address modify not support");
        AssertHelper.IsTrue(oldOrder.Data.Crypto.IsNullOrEmpty() || oldOrder.Data.Crypto == orderDto.Crypto,
            "Order crypto modify not support");
        AssertHelper.IsTrue(oldOrder.Data.CryptoAmount == 0 || oldOrder.Data.CryptoAmount == orderDto.CryptoAmount,
            "Order CryptoAmount modify not support");
        AssertHelper.IsTrue(
            oldOrder.Data.CryptoDecimals == 0 || oldOrder.Data.CryptoDecimals == orderDto.CryptoDecimals,
            "Order CryptoDecimals modify not support");
        AssertHelper.IsTrue(oldOrder.Data.Fiat.IsNullOrEmpty() || oldOrder.Data.Fiat == orderDto.Fiat,
            "Order Fiat modify not support");
        AssertHelper.IsTrue(oldOrder.Data.FiatAmount == 0 || oldOrder.Data.FiatAmount == orderDto.FiatAmount,
            "Order FiatAmount modify not support");
        AssertHelper.IsTrue(
            oldOrder.Data.SettlementAmount.IsNullOrEmpty() ||
            oldOrder.Data.SettlementAmount.SafeToDecimal() == orderDto.SettlementAmount.SafeToDecimal(),
            "Order SettlementAmount modify not support");
        AssertHelper.IsTrue(oldOrder.Data.Network.IsNullOrEmpty() || oldOrder.Data.Network == orderDto.Network,
            "Order Network modify not support");
        AssertHelper.IsTrue(
            oldOrder.Data.ThirdPartName.IsNullOrEmpty() || oldOrder.Data.ThirdPartName == orderDto.ThirdPartName,
            "Order ThirdPartName modify not support");
        AssertHelper.IsTrue(
            oldOrder.Data.ThirdPartOrderId.IsNullOrEmpty() ||
            oldOrder.Data.ThirdPartOrderId == orderDto.ThirdPartOrderId,
            "Order ThirdPartOrderId modify not support");
        AssertHelper.IsTrue(
            oldOrder.Data.ThirdPartCrypto.IsNullOrEmpty() || oldOrder.Data.ThirdPartCrypto == orderDto.ThirdPartCrypto,
            "Order ThirdPartOrderId modify not support");
        AssertHelper.IsTrue(
            oldOrder.Data.ThirdPartCrypto.IsNullOrEmpty() || oldOrder.Data.ThirdPartCrypto == orderDto.ThirdPartCrypto,
            "Order ThirdPartOrderId modify not support");
        var fromStatus = ThirdPartHelper.ParseOrderStatus(oldOrder.Data.Status);
        var newStatus = ThirdPartHelper.ParseOrderStatus(orderDto.Status);
        AssertHelper.IsTrue(oldOrder.Data.Status.IsNullOrEmpty() || OrderStatusTransitions.Reachable(fromStatus, newStatus), "Status unreachable {}-{}",
            fromStatus.ToString(), newStatus.ToString());


        var resp = await treasuryOrderGrain.SaveOrUpdateAsync(orderDto);
        AssertHelper.IsTrue(resp.Success, "Update treasury grain failed: " + resp.Message);
        orderDto = resp.Data;

        _logger.LogDebug("Treasury order publish: {OrderId}-{Version}-{Status}", orderDto.Id, orderDto.Version,
            orderDto.Status);
        await _distributedEventBus.PublishAsync(new TreasuryOrderEto(orderDto));

        externalData ??= new Dictionary<string, string>();
        externalData[ExtensionKey.Version] = orderDto.Version.ToString();
        await _orderStatusProvider.AddOrderStatusInfoAsync(new OrderStatusInfoGrainDto
        {
            OrderId = orderDto.Id,
            ThirdPartOrderNo = orderDto.ThirdPartOrderId,
            OrderStatusInfo = new OrderStatusInfo
            {
                Status = orderDto.Status,
                LastModifyTime = DateTime.UtcNow.ToUtcMilliSeconds(),
                Extension = externalData.IsNullOrEmpty() ? null : JsonConvert.SerializeObject(externalData),
            }
        });

        return orderDto;
    }

    public async Task<PendingTreasuryOrderDto> AddOrUpdatePendingTreasuryOrderAsync(
        PendingTreasuryOrderDto pendingTreasuryOrderDto)
    {
        var pendingOrderGrain = _clusterClient.GetGrain<IPendingTreasuryOrderGrain>(
            IPendingTreasuryOrderGrain.GenerateId(pendingTreasuryOrderDto.ThirdPartName,
                pendingTreasuryOrderDto.ThirdPartOrderId));
        var result = await pendingOrderGrain.AddOrUpdateAsync(pendingTreasuryOrderDto);
        await _distributedEventBus.PublishAsync(new PendingTreasuryOrderEto(result));
        return result;
    }

    public async Task<PagedResultDto<OrderStatusInfoIndex>> QueryOrderStatusInfoPagerAsync(List<string> ids)
    {
        if (ids.IsNullOrEmpty()) return new PagedResultDto<OrderStatusInfoIndex>();
        var mustQuery = new List<Func<QueryContainerDescriptor<OrderStatusInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.OrderId).Terms(ids)));

        QueryContainer Filter(QueryContainerDescriptor<OrderStatusInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, orders) = await _orderStatusInfoRepository.GetSortListAsync(Filter, limit: ids.Count);
        return new PagedResultDto<OrderStatusInfoIndex>(totalCount, orders);
    }

    public async Task<List<TreasuryOrderDto>> ExportOrderAsync(TreasuryOrderCondition condition)
    {
        var result = new List<TreasuryOrderDto>();
        condition.CreateTimeLt ??= long.MaxValue;
        condition.SkipCount = 0;
        condition.MaxResultCount = 100;

        while (true)
        {
            var pageResult = await QueryOrderAsync(condition, s => s.Descending(order => order.CreateTime));
            if (pageResult.Items.IsNullOrEmpty()) break;
            condition.CreateTimeLt = pageResult.Items.Min(order => order.CreateTime);
            result.AddRange(pageResult.Items);
        }

        return result;
    }

    public async Task<PagedResultDto<TreasuryOrderDto>> QueryOrderAsync(TreasuryOrderCondition condition,
        Func<SortDescriptor<TreasuryOrderIndex>, IPromise<IList<ISort>>> customSort = null)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TreasuryOrderIndex>, QueryContainer>>();
        if (!condition.IdIn.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(condition.IdIn)));
        if (!condition.RampOrderIdIn.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.RampOrderId).Terms(condition.RampOrderIdIn)));
        if (!condition.StatusIn.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Status).Terms(condition.StatusIn)));
        if (!condition.ThirdPartIdIn.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.ThirdPartOrderId).Terms(condition.ThirdPartIdIn)));
        if (!condition.CallBackStatusIn.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.CallbackStatus).Terms(condition.CallBackStatusIn)));
        if (!condition.ThirdPartName.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.ThirdPartName).Terms(condition.ThirdPartName)));
        if (!condition.TransferDirection.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.TransferDirection).Terms(condition.TransferDirection)));
        if (!condition.ToAddress.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.ToAddress).Terms(condition.ToAddress)));
        if (!condition.Crypto.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Crypto).Terms(condition.Crypto)));
        if (!condition.TransactionId.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.TransactionId).Terms(condition.TransactionId)));
        if (condition.CallbackCountGtEq != null)
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.CallbackCount).GreaterThanOrEquals(condition.CallbackCountGtEq)));
        if (condition.CallbackCountLt != null)
            mustQuery.Add(q => q.Range(i => i.Field(f => f.CallbackCount).LessThan(condition.CallbackCountLt)));
        if (condition.CreateTimeGtEq != null)
            mustQuery.Add(q => q.Range(i => i.Field(f => f.CreateTime).GreaterThanOrEquals(condition.CreateTimeGtEq)));
        if (condition.CreateTimeLt != null)
            mustQuery.Add(q => q.Range(i => i.Field(f => f.CreateTime).LessThan(condition.CreateTimeLt)));
        if (condition.LastModifyTimeGtEq != null)
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.LastModifyTime).GreaterThanOrEquals(condition.LastModifyTimeGtEq)));
        if (condition.LastModifyTimeLt != null)
            mustQuery.Add(q => q.Range(i => i.Field(f => f.LastModifyTime).LessThan(condition.LastModifyTimeLt)));

        QueryContainer Filter(QueryContainerDescriptor<TreasuryOrderIndex> f) => f.Bool(b => b.Must(mustQuery));

        IPromise<IList<ISort>> Sort(SortDescriptor<TreasuryOrderIndex> s)
        {
            return customSort != null ? customSort(s) : s.Descending(a => a.LastModifyTime);
        }

        var (totalCount, userOrders) =
            await _treasuryOrderRepository.GetSortListAsync(Filter, sortFunc: Sort, limit: condition.MaxResultCount,
                skip: condition.SkipCount);

        var pagerList = _objectMapper.Map<List<TreasuryOrderIndex>, List<TreasuryOrderDto>>(userOrders);
        return new PagedResultDto<TreasuryOrderDto>(totalCount, pagerList);
    }

    public async Task<PagedResultDto<PendingTreasuryOrderDto>> QueryPendingTreasuryOrderAsync(
        PendingTreasuryOrderCondition condition)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<PendingTreasuryOrderIndex>, QueryContainer>>();

        if (!condition.StatusIn.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Status).Terms(condition.StatusIn)));
        if (!condition.ThirdPartNameIn.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.ThirdPartName).Terms(condition.ThirdPartNameIn)));
        if (!condition.ThirdPartOrderId.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.ThirdPartOrderId).Terms(condition.ThirdPartOrderId)));
        if (condition.LastModifyTimeLt != null)
            mustQuery.Add(q => q.Range(i => i.Field(f => f.LastModifyTime).LessThan(condition.LastModifyTimeLt)));
        if (condition.LastModifyTimeGtEq != null)
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.LastModifyTime).GreaterThanOrEquals(condition.LastModifyTimeGtEq)));
        if (condition.ExpireTimeLt != null)
            mustQuery.Add(q => q.Range(i => i.Field(f => f.ExpireTime).LessThan(condition.ExpireTimeLt)));
        if (condition.ExpireTimeGtEq != null)
            mustQuery.Add(q => q.Range(i => i.Field(f => f.ExpireTime).GreaterThanOrEquals(condition.ExpireTimeGtEq)));

        QueryContainer Filter(QueryContainerDescriptor<PendingTreasuryOrderIndex> f) => f.Bool(b => b.Must(mustQuery));
        IPromise<IList<ISort>> Sort(SortDescriptor<PendingTreasuryOrderIndex> s) => s.Descending(a => a.LastModifyTime);

        var (totalCount, userOrders) =
            await _pendingTreasuryOrderRepository.GetSortListAsync(Filter, sortFunc: Sort,
                limit: condition.MaxResultCount,
                skip: condition.SkipCount);

        var pagerList = _objectMapper.Map<List<PendingTreasuryOrderIndex>, List<PendingTreasuryOrderDto>>(userOrders);
        return new PagedResultDto<PendingTreasuryOrderDto>(totalCount, pagerList);
    }
}
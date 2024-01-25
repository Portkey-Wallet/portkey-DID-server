using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Etos;
using Microsoft.Extensions.Logging;
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
    private readonly IOrderStatusProvider _orderStatusProvider;

    public TreasuryOrderProvider(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        INESTRepository<TreasuryOrderIndex, Guid> treasuryOrderRepository, IObjectMapper objectMapper, IOrderStatusProvider orderStatusProvider, ILogger<TreasuryOrderProvider> logger, INESTRepository<PendingTreasuryOrderIndex, Guid> pendingTreasuryOrderRepository)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _treasuryOrderRepository = treasuryOrderRepository;
        _objectMapper = objectMapper;
        _orderStatusProvider = orderStatusProvider;
        _logger = logger;
        _pendingTreasuryOrderRepository = pendingTreasuryOrderRepository;
    }



    public async Task<TreasuryOrderDto> DoSaveOrder(TreasuryOrderDto orderDto, Dictionary<string, string> externalData = null)
    {
        
        var treasuryOrderGrain = _clusterClient.GetGrain<ITreasuryOrderGrain>(orderDto.Id);
        var resp = await treasuryOrderGrain.SaveOrUpdateAsync(orderDto);
        AssertHelper.IsTrue(resp.Success, "Update treasury grain failed: " + resp.Message);
        orderDto = resp.Data;
        
        _logger.LogDebug("Treasury order publish: {OrderId}-{Version}-{Status}", orderDto.Id, orderDto.Version, orderDto.Status);
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
    
    public async Task<PendingTreasuryOrderDto> AddOrUpdatePendingTreasuryOrder(PendingTreasuryOrderDto pendingTreasuryOrderDto)
    {
        var pendingOrderGrain = _clusterClient.GetGrain<IPendingTreasuryOrderGrain>(
            IPendingTreasuryOrderGrain.GenerateId(pendingTreasuryOrderDto.ThirdPartName,
                pendingTreasuryOrderDto.ThirdPartOrderId));
        var result = await pendingOrderGrain.AddOrUpdateAsync(pendingTreasuryOrderDto);
        await _distributedEventBus.PublishAsync(new PendingTreasuryOrderEto(result));
        return result;
    }

    public async Task<PagedResultDto<TreasuryOrderDto>> QueryOrderAsync(TreasuryOrderCondition condition)
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
            mustQuery.Add(q => q.Range(i => i.Field(f => f.LastModifyTime).GreaterThanOrEquals(condition.LastModifyTimeGtEq)));
        if (condition.LastModifyTimeLt != null)
            mustQuery.Add(q => q.Range(i => i.Field(f => f.LastModifyTime).LessThan(condition.LastModifyTimeLt)));

        QueryContainer Filter(QueryContainerDescriptor<TreasuryOrderIndex> f) => f.Bool(b => b.Must(mustQuery));
        IPromise<IList<ISort>> Sort(SortDescriptor<TreasuryOrderIndex> s) => s.Descending(a => a.LastModifyTime);
        
        var (totalCount, userOrders) =
            await _treasuryOrderRepository.GetSortListAsync(Filter, sortFunc: Sort, limit: condition.MaxResultCount,
                skip: condition.SkipCount);

        var pagerList = _objectMapper.Map<List<TreasuryOrderIndex>, List<TreasuryOrderDto>>(userOrders);
        return new PagedResultDto<TreasuryOrderDto>(totalCount, pagerList);
    }

    public async Task<PagedResultDto<PendingTreasuryOrderDto>> QueryPendingTreasuryOrder(PendingTreasuryOrderCondition condition)
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
            mustQuery.Add(q => q.Range(i => i.Field(f => f.LastModifyTime).GreaterThanOrEquals(condition.LastModifyTimeGtEq)));
        if (condition.ExpireTimeLt != null)
            mustQuery.Add(q => q.Range(i => i.Field(f => f.ExpireTime).LessThan(condition.ExpireTimeLt)));
        if (condition.ExpireTimeGtEq != null)
            mustQuery.Add(q => q.Range(i => i.Field(f => f.ExpireTime).GreaterThanOrEquals(condition.ExpireTimeGtEq)));
        
        QueryContainer Filter(QueryContainerDescriptor<PendingTreasuryOrderIndex> f) => f.Bool(b => b.Must(mustQuery));
        IPromise<IList<ISort>> Sort(SortDescriptor<PendingTreasuryOrderIndex> s) => s.Descending(a => a.LastModifyTime);
        
        var (totalCount, userOrders) =
            await _pendingTreasuryOrderRepository.GetSortListAsync(Filter, sortFunc: Sort, limit: condition.MaxResultCount,
                skip: condition.SkipCount);

        var pagerList = _objectMapper.Map<List<PendingTreasuryOrderIndex>, List<PendingTreasuryOrderDto>>(userOrders);
        return new PagedResultDto<PendingTreasuryOrderDto>(totalCount, pagerList);
    }
}
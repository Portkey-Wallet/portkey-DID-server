using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Etos;
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
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<TreasuryOrderIndex, Guid> _treasuryOrderRepository;

    public TreasuryOrderProvider(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        INESTRepository<TreasuryOrderIndex, Guid> treasuryOrderRepository, IObjectMapper objectMapper)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _treasuryOrderRepository = treasuryOrderRepository;
        _objectMapper = objectMapper;
    }


    public async Task DoSaveOrder(TreasuryOrderDto orderDto, Dictionary<string, string> externalData = null)
    {
        var orderStatusGrain = _clusterClient.GetGrain<IOrderStatusInfoGrain>(
            GrainIdHelper.GenerateGrainId(CommonConstant.TreasuryOrderStatusInfoPrefix,
                orderDto.RampOrderId.ToString()));
        await orderStatusGrain.AddOrderStatusInfo(new OrderStatusInfoGrainDto
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

        var treasuryOrderGrain = _clusterClient.GetGrain<ITreasuryOrderGrain>(orderDto.Id);
        await treasuryOrderGrain.SaveOrUpdateAsync(orderDto);
        await _distributedEventBus.PublishAsync(new TreasuryOrderEto(orderDto));
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
}
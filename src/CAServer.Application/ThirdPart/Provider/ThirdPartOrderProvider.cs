using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
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

    private readonly ILogger<ThirdPartOrderProvider> _logger;
    private readonly OrderStatusProvider _orderStatusProvider;
    private readonly INESTRepository<RampOrderIndex, Guid> _orderRepository;
    private readonly INESTRepository<NftOrderIndex, Guid> _nftOrderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ThirdPartOptions _thirdPartOptions;
    private readonly HttpProvider _httpProvider;

    public ThirdPartOrderProvider(INESTRepository<RampOrderIndex, Guid> orderRepository,
        IObjectMapper objectMapper,
        IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus, IOptions<ThirdPartOptions> thirdPartOptions,
        INESTRepository<NftOrderIndex, Guid> nftOrderRepository, ILogger<ThirdPartOrderProvider> logger,
        OrderStatusProvider orderStatusProvider, HttpProvider httpProvider)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _nftOrderRepository = nftOrderRepository;
        _logger = logger;
        _orderStatusProvider = orderStatusProvider;
        _httpProvider = httpProvider;
        _thirdPartOptions = thirdPartOptions.Value;
    }

    // update ramp order
    public async Task<CommonResponseDto<Empty>> UpdateRampOrderAsync(OrderGrainDto dataToBeUpdated)
    {
        AssertHelper.NotEmpty(dataToBeUpdated.Id, "Update order id can not be empty");
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(dataToBeUpdated.Id);
        dataToBeUpdated.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds().ToString();
        _logger.LogInformation("This {ThirdPartName} order {OrderId} will be updated", dataToBeUpdated.MerchantName,
            dataToBeUpdated.Id);

        var result = await orderGrain.UpdateOrderAsync(dataToBeUpdated);
        AssertHelper.IsTrue(result.Success, "Update order error");

        await _orderStatusProvider.AddOrderStatusInfoAsync(
            _objectMapper.Map<OrderGrainDto, OrderStatusInfoGrainDto>(result.Data));

        await _distributedEventBus.PublishAsync(_objectMapper.Map<OrderGrainDto, OrderEto>(result.Data), false);
        return new CommonResponseDto<Empty>();
    }

    // update NFT order
    public async Task<CommonResponseDto<Empty>> UpdateNftOrderAsync(NftOrderGrainDto dataToBeUpdated)
    {
        AssertHelper.NotEmpty(dataToBeUpdated.Id, "Update nft order id can not be empty");
        var nftOrderGrain = _clusterClient.GetGrain<INftOrderGrain>(dataToBeUpdated.Id);
        _logger.LogInformation("This {MerchantName} nft order {OrderId} will be updated", dataToBeUpdated.MerchantName,
            dataToBeUpdated.Id);

        var result = await nftOrderGrain.UpdateNftOrder(dataToBeUpdated);
        AssertHelper.IsTrue(result.Success, "Update nft order error");

        await _distributedEventBus.PublishAsync(_objectMapper.Map<NftOrderGrainDto, NftOrderEto>(result.Data), false);
        return new CommonResponseDto<Empty>();
    }

    // call back NFT order pay result to Merchant webhookUrl
    public async Task<int> CallBackNftOrderPayResultAsync(Guid orderId, string callbackStatus)
    {
        try
        {
            // query nft order grain
            var nftOrderGrain = _clusterClient.GetGrain<INftOrderGrain>(orderId);
            var nftOrderGrainDto = await nftOrderGrain.GetNftOrder();
            AssertHelper.IsTrue(nftOrderGrainDto?.Data?.WebhookStatus == NftOrderWebhookStatus.NONE.ToString(),
                "Webhook status of order {OrderId} exists", orderId);
            if (nftOrderGrainDto?.Data?.WebhookCount >= _thirdPartOptions.Timer.NftCheckoutMerchantCallbackCount)
                return 0;

            // callback merchant and update result
            var grainDto = await DoCallBackNftOrderPayResultAsync(callbackStatus, nftOrderGrainDto?.Data);
            
            grainDto.WebhookTime = DateTime.UtcNow.ToUtcString();
            grainDto.WebhookCount++;
            
            var nftOrderResult = await UpdateNftOrderAsync(grainDto);
            AssertHelper.IsTrue(nftOrderResult.Success,
                "Webhook result update fail, webhookStatus={WebhookStatus}, webhookResult={WebhookResult}",
                grainDto.WebhookStatus, grainDto.WebhookResult);
            return 1;
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "Handle nft order callback fail, Id={Id}, Status={Status}", orderId, callbackStatus);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Handle nft order callback error, Id={Id}, Status={Status}", orderId, callbackStatus);
        }
        return 0;
    }

    private async Task<NftOrderGrainDto> DoCallBackNftOrderPayResultAsync(string callbackStatus,  NftOrderGrainDto nftOrderGrainDto)
    {
        try
        {
            var requestDto = new NftOrderResultRequestDto
            {
                MerchantName = nftOrderGrainDto.MerchantName,
                MerchantOrderId = nftOrderGrainDto.MerchantOrderId,
                OrderId = nftOrderGrainDto.Id.ToString(),
                Status = callbackStatus == OrderStatusType.Pending.ToString()
                    ? NftOrderWebhookStatus.SUCCESS.ToString()
                    : NftOrderWebhookStatus.FAIL.ToString()
            };
            SignMerchantDto(requestDto);

            // do callback merchant
            var res = await _httpProvider.Invoke(HttpMethod.Post, nftOrderGrainDto.WebhookUrl,
                body: JsonConvert.SerializeObject(requestDto, HttpProvider.DefaultJsonSettings));
            nftOrderGrainDto.WebhookResult = res;

            var resObj = JsonConvert.DeserializeObject<CommonResponseDto<Empty>>(res);
            nftOrderGrainDto.WebhookStatus = resObj.Success
                ? NftOrderWebhookStatus.SUCCESS.ToString()
                : NftOrderWebhookStatus.FAIL.ToString();
            
        }
        catch (HttpRequestException e)
        {
            _logger.LogWarning(e, "Do callback nft order fail, Id={Id}, Status={Status}",
                nftOrderGrainDto.Id, callbackStatus);
            nftOrderGrainDto.WebhookStatus = NftOrderWebhookStatus.FAIL.ToString();
            nftOrderGrainDto.WebhookResult = e.Message;
        }
        return nftOrderGrainDto;
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
    
    
    public async Task<PageResultDto<OrderDto>> GetThirdPartOrdersByPageAsync(GetThirdPartOrderConditionDto condition,
        params OrderSectionEnum?[] withSections)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RampOrderIndex>, QueryContainer>>() { };
        if (condition.UserId != Guid.Empty)
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.UserId).Terms(condition.UserId)));

        if (!condition.OrderIdIn.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(condition.OrderIdIn)));

        if (!condition.TransDirectIn.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.TransDirect).Terms(condition.TransDirectIn)));

        if (!condition.StatusIn.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Status).Terms(condition.StatusIn)));

        if (!condition.LastModifyTimeLt.IsNullOrEmpty())
            mustQuery.Add(q => q.TermRange(i => i.Field(f => f.LastModifyTime).LessThan(condition.LastModifyTimeLt)));

        QueryContainer Filter(QueryContainerDescriptor<RampOrderIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        IPromise<IList<ISort>> Sort(SortDescriptor<RampOrderIndex> s) => s.Descending(a => a.LastModifyTime);

        var (totalCount, userOrders) =
            await _orderRepository.GetSortListAsync(Filter, sortFunc: Sort, limit: condition.MaxResultCount,
                skip: condition.SkipCount);

        var pager = new PageResultDto<OrderDto>(
            userOrders.Select(i => _objectMapper.Map<RampOrderIndex, OrderDto>(i)).ToList(), totalCount);

        if (!pager.Data.IsNullOrEmpty() && withSections.Contains(OrderSectionEnum.NftSection))
        {
            var nftOrderPager = await QueryNftOrderPagerAsync(new NftOrderQueryConditionDto(0, pager.Data.Count)
            {
                IdIn = pager.Data.Where(order => NftTransDirect.Contains(order.TransDirect)).Select(order => order.Id)
                    .ToList()
            });
            MergeNftOrderSection(pager, nftOrderPager);
        }

        return pager;
    }

    // query full order with nft-order section
    public async Task<PageResultDto<OrderDto>> GetNftOrdersByPageAsync(NftOrderQueryConditionDto condition)
    {
        var nftOrderPager = await QueryNftOrderPagerAsync(condition);
        if (nftOrderPager.Data.IsNullOrEmpty()) return new PageResultDto<OrderDto>();
        
        var orderIds = nftOrderPager.Data.Select(order => order.Id).ToList();
        var orderPager = await GetThirdPartOrdersByPageAsync(new GetThirdPartOrderConditionDto(0, orderIds.Count)
        {
            OrderIdIn = orderIds
        });
        MergeNftOrderSection(orderPager, nftOrderPager);
        return orderPager;
    }


    // query nft-order index
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

    private void MergeNftOrderSection(PageResultDto<OrderDto> orderPager, PageResultDto<NftOrderIndex> nftOrderPager)
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


    public void SignMerchantDto(NftMerchantBaseDto input)
    {
        var primaryKey = _thirdPartOptions.Merchant.DidPrivateKey.GetValueOrDefault(input.MerchantName);
        input.Signature = MerchantSignatureHelper.GetSignature(primaryKey, input);
    }

    public void VerifyMerchantSignature(NftMerchantBaseDto input)
    {
        var publicKey = _thirdPartOptions.Merchant.MerchantPublicKey.GetValueOrDefault(input.MerchantName);
        AssertHelper.NotEmpty(publicKey, "Invalid merchantName");
        AssertHelper.IsTrue(MerchantSignatureHelper.VerifySignature(publicKey, input.Signature, input),
            "Invalid merchant signature");
    }
}
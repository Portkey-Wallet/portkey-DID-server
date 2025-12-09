using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Service;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.Search;
using CAServer.Signature.Provider;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
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
    private readonly INESTRepository<RampOrderIndex, Guid> _orderRepository;
    private readonly INESTRepository<NftOrderIndex, Guid> _nftOrderRepository;
    private readonly INESTRepository<OrderStatusInfoIndex, string> _orderStatusInfoRepository;
    private readonly INESTRepository<OrderSettlementIndex, Guid> _orderSettlementRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly ISignatureProvider _signatureProvider;
    private readonly ChainOptions _chainOptions;
    
    public ThirdPartOrderProvider(
        INESTRepository<RampOrderIndex, Guid> orderRepository,
        IObjectMapper objectMapper,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        INESTRepository<NftOrderIndex, Guid> nftOrderRepository,
        ILogger<ThirdPartOrderProvider> logger, INESTRepository<OrderStatusInfoIndex, string> orderStatusInfoRepository,
        INESTRepository<OrderSettlementIndex, Guid> orderSettlementRepository, ISignatureProvider signatureProvider,
        IOptionsSnapshot<ChainOptions> chainOptions)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
        _nftOrderRepository = nftOrderRepository;
        _logger = logger;
        _orderStatusInfoRepository = orderStatusInfoRepository;
        _orderSettlementRepository = orderSettlementRepository;
        _thirdPartOptions = thirdPartOptions;
        _signatureProvider = signatureProvider;
        _chainOptions = chainOptions.Value;
    }

    public async Task<RampOrderIndex> GetThirdPartOrderIndexAsync(string orderId)
    {
        var resDict = await GetThirdPartOrderIndexAsync(new List<string>() { orderId });
        return resDict.IsNullOrEmpty() ? null : resDict.Values.First();
    }

    public async Task<Dictionary<Guid, RampOrderIndex>> GetThirdPartOrderIndexAsync(List<string> orderIdIn)
    {
        if (orderIdIn.IsNullOrEmpty()) return new Dictionary<Guid, RampOrderIndex>();

        var mustQuery = new List<Func<QueryContainerDescriptor<RampOrderIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(orderIdIn)));

        QueryContainer Filter(QueryContainerDescriptor<RampOrderIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, userOrders) = await _orderRepository.GetListAsync(Filter);

        return totalCount < 1
            ? new Dictionary<Guid, RampOrderIndex>()
            : userOrders.ToDictionary(order => order.Id, order => order);
    }

    public async Task<OrderDto> GetThirdPartOrderAsync(string orderId)
    {
        var orderIndex = await GetThirdPartOrderIndexAsync(orderId);
        return orderIndex == null ? new OrderDto() : _objectMapper.Map<RampOrderIndex, OrderDto>(orderIndex);
    }

    public async Task<List<OrderDto>> GetUnCompletedThirdPartOrdersAsync()
    {
        var transDirectSell = TransferDirectionType.TokenSell.ToString();
        var modifyTimeLt = DateTimeOffset.UtcNow.AddMinutes(_thirdPartOptions.CurrentValue.Timer.HandleUnCompletedOrderMinuteAgo)
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


    public async Task<PagedResultDto<OrderDto>> GetThirdPartOrdersByPageAsync(GetThirdPartOrderConditionDto condition,
        params OrderSectionEnum?[] withSections)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RampOrderIndex>, QueryContainer>>();
        if (condition.UserId != Guid.Empty)
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.UserId).Terms(condition.UserId)));
        
        if (condition.TransactionId.NotNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.TransactionId).Terms(condition.TransactionId)));

        if (!condition.OrderIdIn.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(condition.OrderIdIn)));

        if (!condition.TransDirectIn.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.TransDirect).Terms(condition.TransDirectIn)));

        if (!condition.StatusIn.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Status).Terms(condition.StatusIn)));

        if (!condition.LastModifyTimeLt.IsNullOrEmpty())
            mustQuery.Add(q => q.TermRange(i => i.Field(f => f.LastModifyTime).LessThan(condition.LastModifyTimeLt)));

        if (!condition.LastModifyTimeGt.IsNullOrEmpty())
            mustQuery.Add(q =>
                q.TermRange(i => i.Field(f => f.LastModifyTime).GreaterThan(condition.LastModifyTimeGt)));
        
        if (condition.ThirdPartName.NotNullOrEmpty())
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.MerchantName).Terms(condition.ThirdPartName)));
        
        if (!condition.ThirdPartOrderNoIn.IsNullOrEmpty())
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.ThirdPartOrderNo).Terms(condition.ThirdPartOrderNoIn)));


        QueryContainer Filter(QueryContainerDescriptor<RampOrderIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        // order by LoastModifyTime DESC
        IPromise<IList<ISort>> Sort(SortDescriptor<RampOrderIndex> s) => s.Descending(a => a.LastModifyTime);

        var (totalCount, userOrders) =
            await _orderRepository.GetSortListAsync(Filter, sortFunc: Sort, limit: condition.MaxResultCount,
                skip: condition.SkipCount);

        var pager = new PagedResultDto<OrderDto>(totalCount,
            userOrders.Select(i => _objectMapper.Map<RampOrderIndex, OrderDto>(i)).ToList());
        if (pager.Items.IsNullOrEmpty()) return pager;

        var orderIdIn = pager.Items.Select(order => order.Id).ToList();
        if (withSections.Contains(OrderSectionEnum.NftSection))
        {
            var nftOrderPager = await QueryNftOrderPagerAsync(new NftOrderQueryConditionDto(0, pager.Items.Count)
            {
                IdIn = orderIdIn
            });
            MergeNftOrderSection(pager, nftOrderPager);
        }

        if (withSections.Contains(OrderSectionEnum.SettlementSection))
        {
            var orderSettlementPager =
                await QueryOrderSettlementInfoPagerAsync(orderIdIn.Select(id => id.ToString()).ToList());
            MergeOrderStatusSection(pager, orderSettlementPager);
        }

        if (withSections.Contains(OrderSectionEnum.OrderStateSection))
        {
            var orderStatusPager = await QueryOrderStatusInfoPagerAsync(orderIdIn.Select(id => id.ToString()).ToList());
            MergeOrderStatusSection(pager, orderStatusPager);
        }

        return pager;
    }

    // query full order with nft-order section
    public async Task<PagedResultDto<OrderDto>> GetNftOrdersByPageAsync(NftOrderQueryConditionDto condition)
    {
        var nftOrderPager = await QueryNftOrderPagerAsync(condition);
        if (nftOrderPager.Items.IsNullOrEmpty()) return new PagedResultDto<OrderDto>();

        var orderIds = nftOrderPager.Items.Select(order => order.Id).ToList();
        var orderPager = await GetThirdPartOrdersByPageAsync(new GetThirdPartOrderConditionDto(0, orderIds.Count)
        {
            OrderIdIn = orderIds
        });
        MergeNftOrderSection(orderPager, nftOrderPager);
        return orderPager;
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

    public async Task<PagedResultDto<OrderSettlementIndex>> QueryOrderSettlementInfoPagerAsync(List<string> ids)
    {
        if (ids.IsNullOrEmpty()) return new PagedResultDto<OrderSettlementIndex>();
        var mustQuery = new List<Func<QueryContainerDescriptor<OrderSettlementIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(ids)));

        QueryContainer Filter(QueryContainerDescriptor<OrderSettlementIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, orders) = await _orderSettlementRepository.GetSortListAsync(Filter, limit: ids.Count);
        return new PagedResultDto<OrderSettlementIndex>(totalCount, orders);
    }

    // query nft-order index
    public async Task<PagedResultDto<NftOrderIndex>> QueryNftOrderPagerAsync(NftOrderQueryConditionDto condition)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NftOrderIndex>, QueryContainer>>();

        // by id
        if (!condition.IdIn.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(condition.IdIn)));

        // by NFT symbol
        if (condition.NftSymbol.NotNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.NftSymbol).Terms(condition.NftSymbol)));

        // in expire time 
        if (condition.ExpireTimeGt.NotNullOrEmpty())
            mustQuery.Add(q => q.TermRange(i => i.Field(f => f.ExpireTime).GreaterThan(condition.ExpireTimeGt)));

        // by merchantOrderId
        if (condition.MerchantName.NotNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.MerchantName).Terms(condition.MerchantName)));
        if (!condition.MerchantOrderIdIn.IsNullOrEmpty())
        {
            AssertHelper.NotEmpty(condition.MerchantName, "MerchantName required if MerchantOrderIdIn set.");
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.MerchantOrderId).Terms(condition.MerchantOrderIdIn)));
        }

        // webhook
        if (condition.WebhookCountGtEq != null)
            mustQuery.Add(q =>
                q.LongRange(i => i.Field(f => f.WebhookCount).GreaterThanOrEquals(condition.WebhookCountGtEq)));
        if (condition.WebhookCountLtEq != null)
            mustQuery.Add(q =>
                q.LongRange(i => i.Field(f => f.WebhookCount).LessThanOrEquals(condition.WebhookCountLtEq)));
        if (condition.WebhookStatus.NotNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.WebhookStatus).Terms(condition.WebhookStatus)));
        if (condition.WebhookTimeLt.NotNullOrEmpty())
            mustQuery.Add(q => q.TermRange(i => i.Field(f => f.WebhookTime).LessThan(condition.WebhookTimeLt)));

        // thirdPartNotify
        if (condition.ThirdPartNotifyCountGtEq != null)
            mustQuery.Add(q =>
                q.LongRange(i =>
                    i.Field(f => f.ThirdPartNotifyCount).GreaterThanOrEquals(condition.ThirdPartNotifyCountGtEq)));
        if (condition.ThirdPartNotifyCountLtEq != null)
            mustQuery.Add(q =>
                q.LongRange(i =>
                    i.Field(f => f.ThirdPartNotifyCount).LessThanOrEquals(condition.ThirdPartNotifyCountLtEq)));
        if (condition.ThirdPartNotifyStatus.NotNullOrEmpty())
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.ThirdPartNotifyStatus).Terms(condition.ThirdPartNotifyStatus)));

        IPromise<IList<ISort>> Sort(SortDescriptor<NftOrderIndex> s) => s.Descending(a => a.CreateTime);
        QueryContainer Filter(QueryContainerDescriptor<NftOrderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, nftOrders) = await _nftOrderRepository.GetSortListAsync(Filter, sortFunc: Sort,
            limit: condition.MaxResultCount, skip: condition.SkipCount);
        return new PagedResultDto<NftOrderIndex>(totalCount, nftOrders);
    }

    public Task UpdateOrderAsync(OrderDto orderDto)
    {
        throw new NotImplementedException();
    }

    private void MergeNftOrderSection(PagedResultDto<OrderDto> orderPager, PagedResultDto<NftOrderIndex> nftOrderPager)
    {
        if (nftOrderPager.Items.IsNullOrEmpty()) return;
        var nftOrderIndices = nftOrderPager.Items.ToDictionary(order => order.Id, order => order);
        foreach (var orderDto in orderPager.Items)
        {
            if (!nftOrderIndices.ContainsKey(orderDto.Id)) continue;
            var nftOrderIndex = nftOrderIndices[orderDto.Id];
            var nftOrderSection = _objectMapper.Map<NftOrderIndex, NftOrderSectionDto>(nftOrderIndex);
            orderDto.NftOrderSection = nftOrderSection;
        }
    }

    private void MergeOrderStatusSection(PagedResultDto<OrderDto> orderPager,
        PagedResultDto<OrderStatusInfoIndex> orderStatusPager)
    {
        if (orderStatusPager.Items.IsNullOrEmpty()) return;
        var statusIndexes = orderStatusPager.Items.ToDictionary(order => order.OrderId, order => order);
        foreach (var orderDto in orderPager.Items)
        {
            if (!statusIndexes.ContainsKey(orderDto.Id)) continue;
            var orderStatusIndex = statusIndexes[orderDto.Id];
            var orderStatusSection = _objectMapper.Map<OrderStatusInfoIndex, OrderStatusSection>(orderStatusIndex);
            orderDto.OrderStatusSection = orderStatusSection;
        }
    }

    private void MergeOrderStatusSection(PagedResultDto<OrderDto> orderPager,
        PagedResultDto<OrderSettlementIndex> orderStatusPager)
    {
        if (orderStatusPager.Items.IsNullOrEmpty()) return;
        var statusIndexes = orderStatusPager.Items.ToDictionary(order => order.Id, order => order);
        foreach (var orderDto in orderPager.Items)
        {
            if (!statusIndexes.ContainsKey(orderDto.Id)) continue;
            var orderStatusIndex = statusIndexes[orderDto.Id];
            var orderStatusSection =
                _objectMapper.Map<OrderSettlementIndex, OrderSettlementSectionDto>(orderStatusIndex);
            orderDto.OrderSettlementSection = orderStatusSection;
        }
    }


    public async void SignMerchantDto(NftMerchantBaseDto input)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(CommonConstant.MainChainId, out var chainInfo))
        {
            return;
        }

        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPubKey(chainInfo.PublicKey);
        var txWithSign = await _signatureProvider.SignTxMsg(ownAddress, MerchantSignatureHelper.GetRawData(input));

        input.Signature = ByteStringHelper.FromHexString(txWithSign).ToString();
    }

    public void VerifyMerchantSignature(NftMerchantBaseDto input)
    {
        try
        {
            AssertHelper.NotEmpty(input.Signature, "Empty input signature");

            var merchantOption = _thirdPartOptions.CurrentValue.Merchant.GetOption(input.MerchantName);
            AssertHelper.NotEmpty(merchantOption?.PublicKey, "Merchant {Merchant} public key empty",
                input.MerchantName);

            var rawData = ThirdPartHelper.ConvertObjectToSortedString(input, MerchantSignatureHelper.SignatureField);
            var signatureValid =
                MerchantSignatureHelper.VerifySignature(merchantOption?.PublicKey, input.Signature, rawData);
            if (!signatureValid)
                _logger.LogWarning(
                    "Verify merchant {Name} signature failed, inputSignature={Signature}, rawData={RawData}",
                    input.MerchantName, input.Signature, rawData);
            AssertHelper.IsTrue(signatureValid, "Invalid merchant signature");
        }
        catch (UserFriendlyException e)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Verify merchant signature failed");
            throw new UserFriendlyException("Verify merchant signature failed");
        }
    }
}
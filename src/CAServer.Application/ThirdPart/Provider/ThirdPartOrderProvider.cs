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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
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
    private readonly IObjectMapper _objectMapper;
    private readonly ThirdPartOptions _thirdPartOptions;

    public ThirdPartOrderProvider(
        INESTRepository<RampOrderIndex, Guid> orderRepository,
        IObjectMapper objectMapper,
        IOptions<ThirdPartOptions> thirdPartOptions,
        INESTRepository<NftOrderIndex, Guid> nftOrderRepository,
        ILogger<ThirdPartOrderProvider> logger
    )
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
        _nftOrderRepository = nftOrderRepository;
        _logger = logger;
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
        var transDirectSell = TransferDirectionType.TokenSell.ToString();
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

        if (!condition.LastModifyTimeGt.IsNullOrEmpty())
            mustQuery.Add(q =>
                q.TermRange(i => i.Field(f => f.LastModifyTime).GreaterThan(condition.LastModifyTimeGt)));

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
        AssertHelper.NotEmpty(input.Signature, "Empty iput signature");
        var rawData = ThirdPartHelper.ConvertObjectToSortedString(input, MerchantSignatureHelper.SignatureField);
        var signatureValid = MerchantSignatureHelper.VerifySignature(publicKey, input.Signature, rawData);
        if (!signatureValid)
            _logger.LogWarning("Verify merchant {Name} signature failed, inputSignature={Signature}, rawData={RawData}",
                input.MerchantName, input.Signature, rawData);
        AssertHelper.IsTrue(signatureValid, "Invalid merchant signature");
    }
}
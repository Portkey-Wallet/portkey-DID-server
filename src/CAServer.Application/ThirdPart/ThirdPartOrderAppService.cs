using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Adaptor;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Processor;
using CAServer.ThirdPart.Provider;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace CAServer.ThirdPart;

public partial class ThirdPartOrderAppService : CAServerAppService, IThirdPartOrderAppService, ISingletonDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<ThirdPartOrderAppService> _logger;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IActivityProvider _activityProvider;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IOrderStatusProvider _orderStatusProvider;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IOptionsMonitor<RampOptions> _rampOptions;
    private readonly Dictionary<string, IThirdPartAdaptor> _thirdPartAdaptors;
    private readonly Dictionary<string, AbstractRampOrderProcessor> _rampOrderProcessors;

    public ThirdPartOrderAppService(IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        IThirdPartOrderProvider thirdPartOrderProvider,
        ILogger<ThirdPartOrderAppService> logger,
        IObjectMapper objectMapper,
        IActivityProvider activityProvider,
        IOrderStatusProvider orderStatusProvider,
        IAbpDistributedLock distributedLock, IOptionsMonitor<RampOptions> rampOptions,
        IEnumerable<IThirdPartAdaptor> thirdPartAdaptors,
        IEnumerable<AbstractRampOrderProcessor> rampOrderProcessors)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _objectMapper = objectMapper;
        _logger = logger;
        _activityProvider = activityProvider;
        _thirdPartOptions = thirdPartOptions;
        _orderStatusProvider = orderStatusProvider;
        _distributedLock = distributedLock;
        _rampOptions = rampOptions;
        _thirdPartAdaptors = thirdPartAdaptors.ToDictionary(a => a.ThirdPart(), a => a);
        _rampOrderProcessors = rampOrderProcessors.ToDictionary(p => p.MerchantName(), p => p);
    }

    private AbstractRampOrderProcessor GetThirdPartOrderProcessor(string thirdPart)
    {
        var processorExists = _rampOrderProcessors.TryGetValue(thirdPart, out var processor);
        AssertHelper.IsTrue(processorExists, "Order processor of {ThirdPart} not exists", thirdPart);
        AssertHelper.NotNull(processor, "Order processor of {ThirdPart} null", thirdPart);
        return processor;
    }

    // crate user ramp order
    public async Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input)
    {
        // var userId = input.UserId;
        var userId = CurrentUser.GetId();
        var orderId = GuidGenerator.Create();
        try
        {
            var orderGrainData = _objectMapper.Map<CreateUserOrderDto, OrderGrainDto>(input);
            orderGrainData.Id = orderId;
            orderGrainData.UserId = userId;
            orderGrainData.Status = OrderStatusType.Initialized.ToString();
            orderGrainData.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds().ToString();

            var result = await DoCreateOrderAsync(orderGrainData);

            var resp = _objectMapper.Map<OrderGrainDto, OrderCreatedDto>(result.Data);
            resp.Success = true;
            return resp;
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "create ramp order error, orderId={OrderId}, userId={UserId}",
                orderId, userId);
        }

        return new OrderCreatedDto();
    }

    public async Task<CommonResponseDto<OrderDto>> QueryThirdPartRampOrder(OrderDto orderDto)
    {
        try
        {
            var orderResp = await GetThirdPartOrderProcessor(orderDto.MerchantName).QueryThirdOrderAsync(orderDto);
            return new CommonResponseDto<OrderDto>(orderResp);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Ramp thirdPart update order error");
            return new CommonResponseDto<OrderDto>().Error(e);
        }
    }

    public async Task<CommonResponseDto<Empty>> OrderUpdateAsync(string thirdPart, IThirdPartOrder thirdPartOrder)
    {
        try
        {
            return await GetThirdPartOrderProcessor(thirdPart).OrderUpdateAsync(thirdPartOrder);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Ramp thirdPart update order error");
            return new CommonResponseDto<Empty>().Error(e);
        }
    }

    public async Task UpdateOffRampTxHash(TransactionHashDto input)
    {
        try
        {
            AssertHelper.NotNull(input, "input null");
            AssertHelper.NotEmpty(input.MerchantName, "MerchantName empty");
            AssertHelper.NotEmpty(input.OrderId, "OrderId empty");
            AssertHelper.NotEmpty(input.TxHash, "TxHash empty");
            await GetThirdPartOrderProcessor(input.MerchantName).UpdateTxHashAsync(input);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "UpdateTxHashAsync error");
        }
    }
    

    // create base order with nft-order section
    public async Task<CommonResponseDto<CreateNftOrderResponseDto>> CreateNftOrderAsync(CreateNftOrderRequestDto input)
    {
        try
        {
            AssertHelper.NotNull(input, "create Param null");
            _thirdPartOrderProvider.VerifyMerchantSignature(input);

            // Query userId from caHolder
            var caHolder = await _activityProvider.GetCaHolder(input.CaHash);

            // query decimal of paymentSymbol via GraphQL
            var decimalsList = await _activityProvider.GetTokenDecimalsAsync(input.PaymentSymbol);
            AssertHelper.NotEmpty(decimalsList?.TokenInfo, "Price symbol of {PriceSymbol} decimal not found",
                input.PaymentSymbol);
            AssertHelper.IsTrue(double.TryParse(input.PaymentAmount, out var priceAmount), "Invalid priceAmount");

            var merchantAddress = input.MerchantAddress.DefaultIfEmpty(
                _thirdPartOptions.CurrentValue.Merchant.GetOption(input.MerchantName).ReceivingAddress);
            AssertHelper.NotEmpty(merchantAddress, "Merchant crypto settlement address empty");

            // Save ramp order
            var orderGrainData = _objectMapper.Map<CreateNftOrderRequestDto, OrderGrainDto>(input);
            orderGrainData.Id = GuidHelper.UniqId(input.MerchantName, input.MerchantOrderId);
            orderGrainData.UserId = caHolder?.UserId ?? Guid.Empty;
            orderGrainData.Status = OrderStatusType.Initialized.ToString();
            orderGrainData.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds().ToString();
            orderGrainData.CryptoPrice =
                (priceAmount / Math.Pow(10, decimalsList.TokenInfo[0].Decimals)).ToString(CultureInfo.InvariantCulture);
            var createResult = await DoCreateOrderAsync(orderGrainData);
            AssertHelper.IsTrue(createResult.Success, "Create main order failed: " + createResult.Message);

            // save nft order section
            var nftOrderGrainDto = _objectMapper.Map<CreateNftOrderRequestDto, NftOrderGrainDto>(input);
            nftOrderGrainDto.Id = createResult.Data.Id;
            nftOrderGrainDto.MerchantAddress = merchantAddress;
            var createNftResult = await DoCreateNftOrderAsync(nftOrderGrainDto);
            AssertHelper.IsTrue(createResult.Success, "Order save failed");

            var resp = new CreateNftOrderResponseDto
            {
                MerchantName = createNftResult.Data.MerchantName,
                MerchantAddress = nftOrderGrainDto.MerchantAddress,
                OrderId = createNftResult.Data.Id.ToString(),
            };
            _thirdPartOrderProvider.SignMerchantDto(resp);

            return new CommonResponseDto<CreateNftOrderResponseDto>(resp);
        }
        catch (UserFriendlyException e)
        {
            Logger.LogWarning(e, "create nftOrder failed, orderId={OrderId}, merchantName={MerchantName}",
                input.MerchantOrderId, input.MerchantName);
            return new CommonResponseDto<CreateNftOrderResponseDto>().Error(e);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "create nftOrder error, orderId={OrderId}, merchantName={MerchantName}",
                input.MerchantOrderId, input.MerchantName);
            return new CommonResponseDto<CreateNftOrderResponseDto>().Error(e, "Internal error");
        }
    }

    // create ramp order
    private async Task<GrainResultDto<OrderGrainDto>> DoCreateOrderAsync(OrderGrainDto orderGrainDto)
    {
        _logger.LogInformation("This third part order {OrderId} of user:{UserId} will be created",
            orderGrainDto.ThirdPartOrderNo, orderGrainDto.UserId);

        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderGrainDto.Id);
        var result = await orderGrain.CreateUserOrderAsync(orderGrainDto);
        AssertHelper.IsTrue(result.Success, "Create user order fail :" + result.Message);

        await _distributedEventBus.PublishAsync(_objectMapper.Map<OrderGrainDto, OrderEto>(result.Data));
        await _orderStatusProvider.AddOrderStatusInfoAsync(
            _objectMapper.Map<OrderGrainDto, OrderStatusInfoGrainDto>(result.Data));
        return result;
    }

    // create nft-order
    private async Task<GrainResultDto<NftOrderGrainDto>> DoCreateNftOrderAsync(NftOrderGrainDto nftOrderGrainDto)
    {
        _logger.LogInformation("This nft-order {OrderId} of merchant:{MerchantName} will be created",
            nftOrderGrainDto.MerchantOrderId, nftOrderGrainDto.MerchantName);
        nftOrderGrainDto.CreateTime = DateTime.UtcNow;
        nftOrderGrainDto.ExpireTime = DateTime.UtcNow.AddSeconds(_thirdPartOptions.CurrentValue.Timer.NftOrderExpireSeconds);
        var nftOrderGrain = _clusterClient.GetGrain<INftOrderGrain>(nftOrderGrainDto.Id);
        var result = await nftOrderGrain.CreateNftOrder(nftOrderGrainDto);
        AssertHelper.IsTrue(result.Success, "Create merchant nft-order fail");

        await _distributedEventBus.PublishAsync(new NftOrderEto(nftOrderGrainDto));
        return result;
    }

    // query by merchantName & merchantId with MerchantSignature
    public async Task<CommonResponseDto<NftOrderQueryResponseDto>> QueryMerchantNftOrderAsync(
        OrderQueryRequestDto input)
    {
        try
        {
            AssertHelper.IsTrue(!input.MerchantOrderId.IsNullOrEmpty() && !input.MerchantName.IsNullOrEmpty(),
                "merchantOrderId and merchantName can not be empty");

            // query full order with nft-order-section
            var orderPager = await _thirdPartOrderProvider.GetNftOrdersByPageAsync(new NftOrderQueryConditionDto(0, 1)
            {
                MerchantName = input.MerchantName,
                MerchantOrderIdIn = new List<string> { input.MerchantOrderId }
            });
            if (orderPager.Data.IsNullOrEmpty()) return new CommonResponseDto<NftOrderQueryResponseDto>();

            // get and verify nft-order-section
            var orderDto = orderPager.Data[0];
            var nftOrderSection = orderDto.NftOrderSection;
            AssertHelper.NotNull(nftOrderSection, "invalid nft order data, orderId={OrderId}", orderDto.Id);

            // convert response
            var orderQueryResponseDto =
                _objectMapper.Map<NftOrderSectionDto, NftOrderQueryResponseDto>(nftOrderSection);
            orderQueryResponseDto.Status = orderDto.Status;
            orderQueryResponseDto.PaymentSymbol = orderDto.Crypto;
            orderQueryResponseDto.PaymentAmount = orderDto.CryptoAmount;

            _thirdPartOrderProvider.SignMerchantDto(orderQueryResponseDto);
            return new CommonResponseDto<NftOrderQueryResponseDto>(orderQueryResponseDto);
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "QueryMerchantNftOrderAsync fail");
            return new CommonResponseDto<NftOrderQueryResponseDto>().Error(e);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "QueryMerchantNftOrderAsync fail");
            return new CommonResponseDto<NftOrderQueryResponseDto>().Error(e, "Internal error please try again later.");
        }
    }

    public async Task<PageResultDto<OrderDto>> GetThirdPartOrdersAsync(GetUserOrdersDto input)
    {
        // var userId = input.UserId;
        var userId = CurrentUser.Id == null ? Guid.Empty : CurrentUser.GetId();
        var orderIdIn = input.OrderId == Guid.Empty ? null : new List<Guid> { input.OrderId };
        return await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(
            new GetThirdPartOrderConditionDto(input.SkipCount, input.MaxResultCount)
            {
                UserId = userId,
                OrderIdIn = orderIdIn
            }, OrderSectionEnum.NftSection);
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Processors;
using CAServer.ThirdPart.Provider;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace CAServer.ThirdPart;

public class ThirdPartOrderAppService : CAServerAppService, IThirdPartOrderAppService, ISingletonDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<ThirdPartOrderAppService> _logger;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IActivityProvider _activityProvider;
    private readonly ThirdPartOptions _thirdPartOptions;
    private IOrderStatusProvider _orderStatusProvider;

    public ThirdPartOrderAppService(IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        IOptions<ThirdPartOptions> thirdPartOptions,
        IThirdPartOrderProvider thirdPartOrderProvider,
        ILogger<ThirdPartOrderAppService> logger,
        IObjectMapper objectMapper,
        IActivityProvider activityProvider,
        IOrderStatusProvider orderStatusProvider)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _objectMapper = objectMapper;
        _logger = logger;
        _activityProvider = activityProvider;
        _thirdPartOptions = thirdPartOptions.Value;
        _orderStatusProvider = orderStatusProvider;
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

    // create base order with nft-order section
    public async Task<CommonResponseDto<CreateNftOrderResponseDto>> CreateNftOrderAsync(CreateNftOrderRequestDto input)
    {
        try
        {
            AssertHelper.NotNull(input, "create Param null");
            _thirdPartOrderProvider.VerifyMerchantSignature(input);

            var caHolder = await _activityProvider.GetCaHolder(input.CaHash);

            // save ramp order
            var orderGrainData = _objectMapper.Map<CreateNftOrderRequestDto, OrderGrainDto>(input);
            orderGrainData.Id = GuidHelper.UniqId(input.MerchantName, input.MerchantOrderId);
            orderGrainData.UserId = caHolder?.UserId ?? Guid.Empty;
            orderGrainData.Status = OrderStatusType.Initialized.ToString();
            orderGrainData.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds().ToString();
            var createResult = await DoCreateOrderAsync(orderGrainData);

            // save nft order section
            var nftOrderGrainDto = _objectMapper.Map<CreateNftOrderRequestDto, NftOrderGrainDto>(input);
            nftOrderGrainDto.Id = createResult.Data.Id;
            var createNftResult = await DoCreateNftOrderAsync(nftOrderGrainDto);
            AssertHelper.IsTrue(createResult.Success, "Order save failed");

            var resp = new CreateNftOrderResponseDto
            {
                MerchantName = createNftResult.Data.MerchantName,
                OrderId = createNftResult.Data.Id.ToString()
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
            _thirdPartOrderProvider.VerifyMerchantSignature(input);
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
            var nftOrder = orderDto.OrderSections?.GetValueOrDefault(OrderSectionEnum.NftSection.ToString());
            AssertHelper.NotNull(nftOrder, "invalid nft order data, orderId={OrderId}", orderDto.Id);
            AssertHelper.NotNull(nftOrder is NftOrderSectionDto, "invalid nft order type, orderId={OrderId}",
                orderDto.Id);

            // convert response
            var nftOrderSection = nftOrder as NftOrderSectionDto;
            var orderQueryResponseDto =
                _objectMapper.Map<NftOrderSectionDto, NftOrderQueryResponseDto>(nftOrderSection);
            orderQueryResponseDto.Status = orderDto.Status;

            // sign response
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

    public async Task<CommonResponseDto<Empty>> NoticeNftReleaseResultAsync(NftReleaseResultRequestDto input)
    {
        try
        {
            _thirdPartOrderProvider.VerifyMerchantSignature(input);

            // search ES for orderId
            var nftOrderPager = await _thirdPartOrderProvider.GetNftOrdersByPageAsync(
                new NftOrderQueryConditionDto(0, 1)
                {
                    MerchantName = input.MerchantName,
                    MerchantOrderIdIn = new List<string> { input.MerchantOrderId }
                });
            AssertHelper.NotEmpty(nftOrderPager.Data, "Order {OrderId} of {Merchant} not found", input.MerchantOrderId,
                input.MerchantName);
            var orderIndex = nftOrderPager.Data[0];
            var orderId = orderIndex.Id;

            // query verify order grain
            var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);
            var orderGrainDto = (await orderGrain.GetOrder()).Data;
            AssertHelper.NotNull(orderGrainDto, "No order found for {OrderId}", orderId);

            // calculate new status and check
            var nextStatus = input.ReleaseResult == NftReleaseResult.SUCCESS.ToString()
                ? OrderStatusType.Finish
                : OrderStatusType.TransferFailed;
            var currentStatus = ThirdPartHelper.ParseOrderStatus(orderGrainDto.Status);
            AssertHelper.IsTrue(OrderStatusTransitions.Reachable(currentStatus, nextStatus),
                "Status {Next} unreachable from {Current}", nextStatus, currentStatus);

            // update base-order status 
            orderGrainDto.Status = nextStatus.ToString();
            orderGrainDto.TransactionId = input.ReleaseTransactionId;
            var orderUpdateResult = await _orderStatusProvider.UpdateRampOrderAsync(orderGrainDto);
            AssertHelper.IsTrue(orderUpdateResult.Success, "Update ramp order fail");

            return new CommonResponseDto<Empty>();
        }
        catch (UserFriendlyException e)
        {
            Logger.LogWarning(e, "Notice NFT release result failed, orderId={OrderId}, merchantName={MerchantName}",
                input.MerchantOrderId, input.MerchantName);
            return new CommonResponseDto<Empty>().Error(e);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Notice NFT release result error, orderId={OrderId}, merchantName={MerchantName}",
                input.MerchantOrderId, input.MerchantName);
            return new CommonResponseDto<Empty>().Error(e, "Internal error");
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
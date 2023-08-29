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
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace CAServer.ThirdPart;

public class ThirdPartOrderAppService : CAServerAppService, IThirdPartOrderAppService
{
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<ThirdPartOrderAppService> _logger;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;
    private readonly IActivityProvider _activityProvider;
    private readonly MerchantOptions _merchantOptions;
    private readonly IThirdPartOrderProcessorFactory _thirdPartOrderProcessorFactory;

    public ThirdPartOrderAppService(IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        IThirdPartOrderProvider thirdPartOrderProvider,
        ILogger<ThirdPartOrderAppService> logger,
        IObjectMapper objectMapper, IOptions<ThirdPartOptions> thirdPartOptions,
        IOrderStatusProvider orderStatusProvider, IActivityProvider activityProvider,
        IThirdPartOrderProcessorFactory thirdPartOrderProcessorFactory)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _objectMapper = objectMapper;
        _logger = logger;
        _orderStatusProvider = orderStatusProvider;
        _activityProvider = activityProvider;
        _thirdPartOrderProcessorFactory = thirdPartOrderProcessorFactory;
        _merchantOptions = thirdPartOptions.Value.Merchant;
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
        catch (UserFriendlyException e)
        {
            Logger.LogWarning(e, "create ramp order failed, orderId={OrderId}, userId={UserId}",
                orderId, userId);
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
            _thirdPartOrderProvider.VerifyMerchantSignature(input);

            var caHolder = await _activityProvider.GetCaHolder(input.CaHash);
            AssertHelper.NotNull(caHolder, "caHash {CaHash} not found", input.CaHash);

            // save ramp order
            var orderGrainData = _objectMapper.Map<CreateNftOrderRequestDto, OrderGrainDto>(input);
            orderGrainData.Id = GuidHelper.UniqId(input.MerchantName, input.MerchantOrderId);
            orderGrainData.UserId = caHolder.UserId;
            orderGrainData.Status = OrderStatusType.Initialized.ToString();
            orderGrainData.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds().ToString();
            var createResult = await DoCreateOrderAsync(orderGrainData);

            // save nft order section
            var nftOrderGrainDto = _objectMapper.Map<CreateNftOrderRequestDto, NftOrderGrainDto>(input);
            nftOrderGrainDto.Id = createResult.Data.Id;
            var createNftResult = await DoCreateNftOrderAsync(nftOrderGrainDto);

            return new CommonResponseDto<CreateNftOrderResponseDto>(new CreateNftOrderResponseDto
            {
                OrderId = createResult.Data.Id.ToString()
            });
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
        AssertHelper.IsTrue(result.Success, "Create user order fail");

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

        await _distributedEventBus.PublishAsync(_objectMapper.Map<NftOrderGrainDto, NftOrderEto>(nftOrderGrainDto));
        return result;
    }

    // query by merchantName & merchantId with MerchantSignature
    public async Task<CommonResponseDto<OrderQueryResponseDto>> QueryMerchantNftOrderAsync(OrderQueryRequestDto input)
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
        if (orderPager.Data.IsNullOrEmpty()) return new CommonResponseDto<OrderQueryResponseDto>();

        // get and verify nft-order-section
        var orderDto = orderPager.Data[0];
        var nftOrder = orderDto.OrderSections?.GetValueOrDefault(OrderSectionEnum.NftSection.ToString());
        AssertHelper.NotNull(nftOrder, "invalid nft order data, orderId={OrderId}", orderDto.Id);
        AssertHelper.NotNull(nftOrder is NftOrderSectionDto, "invalid nft order type, orderId={OrderId}", orderDto.Id);

        // convert response
        var nftOrderSection = nftOrder as NftOrderSectionDto;
        var orderQueryResponseDto = _objectMapper.Map<NftOrderSectionDto, OrderQueryResponseDto>(nftOrderSection);
        _objectMapper.Map(orderDto, orderQueryResponseDto);

        // sign response
        _thirdPartOrderProvider.SignMerchantDto(orderQueryResponseDto);
        return new CommonResponseDto<OrderQueryResponseDto>(orderQueryResponseDto);
    }

    public async Task<CommonResponseDto<Empty>> NoticeNftReleaseResultAsync(NftReleaseResultRequestDto input)
    {
        _thirdPartOrderProvider.VerifyMerchantSignature(input);
        
        var nftOrderPager = await _thirdPartOrderProvider.GetNftOrdersByPageAsync(new NftOrderQueryConditionDto(0, 1)
        {
            MerchantName = input.MerchantName,
            MerchantOrderIdIn = new List<string> { input.MerchantOrderId }
        });
        AssertHelper.NotEmpty(nftOrderPager.Data, "Order {OrderId} of {Merchant} not found", input.MerchantOrderId,
            input.MerchantName);
        var orderIndex = nftOrderPager.Data[0];
        
        // merchantName in order means ThirdPartName (Alchemy etc.)
        return await _thirdPartOrderProcessorFactory.GetProcessor(orderIndex.MerchantName).NotifyNftReleaseAsync(orderIndex.Id, input);
    }

    public async Task<PageResultDto<OrderDto>> GetThirdPartOrdersAsync(GetUserOrdersDto input)
    {
        // var userId = input.UserId;
        var userId = CurrentUser.GetId();
        return await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(userId, new List<Guid> { input.OrderId },
            input.SkipCount,
            input.MaxResultCount);
    }

}
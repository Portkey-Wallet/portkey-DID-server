using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using AutoResponseWrapper.Response;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
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

    public ThirdPartOrderAppService(IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        IThirdPartOrderProvider thirdPartOrderProvider,
        ILogger<ThirdPartOrderAppService> logger,
        IObjectMapper objectMapper, IOptions<ThirdPartOptions> thirdPartOptions,
        IOrderStatusProvider orderStatusProvider, IActivityProvider activityProvider)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _objectMapper = objectMapper;
        _logger = logger;
        _orderStatusProvider = orderStatusProvider;
        _activityProvider = activityProvider;
        _merchantOptions = thirdPartOptions.Value.Merchant;
    }


    public async Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input)
    {
        // var userId = input.UserId;
        var userId = CurrentUser.GetId();

        var orderId = GuidGenerator.Create();
        var orderGrainData = _objectMapper.Map<CreateUserOrderDto, OrderGrainDto>(input);
        orderGrainData.Id = orderId;
        orderGrainData.UserId = userId;
        orderGrainData.Status = OrderStatusType.Initialized.ToString();
        orderGrainData.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds().ToString();

        var result = await DoCreateOrderAsync(orderGrainData);
        if (!result.Success)
        {
            return new OrderCreatedDto();
        }

        var resp = _objectMapper.Map<OrderGrainDto, OrderCreatedDto>(result.Data);
        resp.Success = true;
        return resp;
    }

    public async Task<ResponseDto> CreateNftOrderAsync(CreateNftOrderRequestDto input)
    {
        try
        {
            var publicKey = _merchantOptions.MerchantPublicKey.GetValueOrDefault(input.MerchantName);
            AssertHelper.NotEmpty(publicKey, "Invalid merchantName");
            AssertHelper.IsTrue(MerchantSignatureHelper.VerifySignature(publicKey, input.Signature, input),
                "Invalid merchant signature");

            var caHolder = await _activityProvider.GetCaHolder(input.CaHash);
            AssertHelper.NotNull(caHolder, "caHash {CaHash} not found", input.CaHash);

            var orderGrainData = _objectMapper.Map<CreateNftOrderRequestDto, OrderGrainDto>(input);
            orderGrainData.Id = GuidHelper.UniqId(input.MerchantName, input.MerchantOrderId);
            orderGrainData.UserId = caHolder.UserId;
            orderGrainData.Status = OrderStatusType.Initialized.ToString();
            orderGrainData.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds().ToString();
            var createResult = await DoCreateOrderAsync(orderGrainData);
            AssertHelper.IsTrue(createResult.Success, "save order fail");


            //TODO nzc nftOrder grain & index

            return new ResponseDto().Success(new CreateNftOrderResponseDto
            {
                OrderId = createResult.Data.Id.ToString()
            });
        }
        catch (UserFriendlyException e)
        {
            Logger.LogWarning(e, "create nftOrder failed, orderId={OrderId}, merchantName={MerchantName}",
                input.MerchantOrderId, input.MerchantName);
            return new ResponseDto().Error(e);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "create nftOrder error, orderId={OrderId}, merchantName={MerchantName}",
                input.MerchantOrderId, input.MerchantName);
            return new ResponseDto().Error(e, "Internal error");
        }
    }

    private async Task<GrainResultDto<OrderGrainDto>> DoCreateOrderAsync(OrderGrainDto orderGrainDto)
    {
        _logger.LogInformation("This third part nft-order {OrderId} will be created", orderGrainDto.ThirdPartOrderNo);

        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderGrainDto.Id);
        var result = await orderGrain.CreateUserOrderAsync(orderGrainDto);
        if (!result.Success)
        {
            _logger.LogError("Create user order fail, order id: {OrderId} user id: {UserId}", orderGrainDto.Id,
                orderGrainDto.UserId);
            return result;
        }

        await _distributedEventBus.PublishAsync(_objectMapper.Map<OrderGrainDto, OrderEto>(result.Data));
        await _orderStatusProvider.AddOrderStatusInfoAsync(
            _objectMapper.Map<OrderGrainDto, OrderStatusInfoGrainDto>(result.Data));
        var resp = _objectMapper.Map<OrderGrainDto, OrderCreatedDto>(result.Data);
        resp.Success = true;
        return result;
    }

    public async Task<ResponseDto> QueryMerchantNftOrderAsync(OrderQueryRequestDto input)
    {
        if (!input.OrderId.IsNullOrEmpty())
        {
            var orderList = await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(Guid.Empty, 
                    Guid.Parse(input.OrderId), 0, 1);
            if (orderList.IsNullOrEmpty()) return new ResponseDto().Success();
            // TODO nzc query nftOrder 
        }

        //TODO nzc query order after query nftOrder
        throw new NotImplementedException();
    }

    public Task<ResponseDto> NoticeNftReleaseResultAsync(NftResultRequestDto input)
    {
        //TODO nzc 
        throw new NotImplementedException();
    }

    public async Task<OrdersDto> GetThirdPartOrdersAsync(GetUserOrdersDto input)
    {
        // var userId = input.UserId;
        var userId = CurrentUser.GetId();

        var orderList =
            await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(userId, input.OrderId, input.SkipCount,
                input.MaxResultCount);
        return new OrdersDto
        {
            TotalRecordCount = orderList.Count,
            Data = orderList
        };
    }
}
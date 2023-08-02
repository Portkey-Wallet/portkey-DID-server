using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace CAServer.ThirdPart.Processors;

public abstract class AbstractOrderProcessor : CAServerAppService, IOrderProcessor, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<AbstractOrderProcessor> _logger;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;

    protected AbstractOrderProcessor(IClusterClient clusterClient, 
        ILogger<AbstractOrderProcessor> logger,
        IThirdPartOrderProvider thirdPartOrderProvider, IDistributedEventBus distributedEventBus,
        IOrderStatusProvider orderStatusProvider, IObjectMapper objectMapper)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _distributedEventBus = distributedEventBus;
        _orderStatusProvider = orderStatusProvider;
        _objectMapper = objectMapper;
    }
    
    protected virtual Guid GenerateGrainId(OrderDto input)
    {
        return ThirdPartHelper.GetOrderId(input.MerchantName, input.ThirdPartOrderNo);
    }
    
    
    protected abstract IThirdPartOrder VerifyOrderInput<T>(T iThirdPartOrder) where T : IThirdPartOrder;

    protected abstract OrderDto ConvertOrderDto<T>(T iThirdPartOrder) where T : IThirdPartOrder;
    
    
    public abstract string MapperOrderStatus(OrderDto orderDto);
    
    public abstract string MerchantName();
    
    public abstract Task UpdateTxHashAsync(TransactionHashDto transactionHashDto);
    
    // query new Third order data, and convert to orderDto
    public abstract Task<OrderDto> QueryThirdOrder(OrderDto orderDto);
    
    
    public async Task<BasicOrderResult> OrderUpdate(IThirdPartOrder thirdPartOrder)
    {
        OrderDto inputOrderDto = null;
        try
        {
            var verifiedOrderData = VerifyOrderInput(thirdPartOrder);
            
            inputOrderDto = ConvertOrderDto(verifiedOrderData);

            Guid grainId = GenerateGrainId(inputOrderDto);
            
            var inputState = MapperOrderStatus(inputOrderDto);

            var esOrderData = await _thirdPartOrderProvider.GetThirdPartOrderAsync(grainId.ToString());
            if (esOrderData == null || inputOrderDto.Id != esOrderData.Id)
                throw new UserFriendlyException($"No order found for {grainId}");
            if (esOrderData.Status == inputState)
                throw new UserFriendlyException($"Order status {inputOrderDto.Status} no need to update.");

            var dataToBeUpdated = MergeEsAndInput2GrainModel(inputOrderDto, esOrderData);
            var orderGrain = _clusterClient.GetGrain<IOrderGrain>(grainId);
            dataToBeUpdated.Status = inputState;
            dataToBeUpdated.Id = grainId;
            dataToBeUpdated.UserId = esOrderData.UserId;
            dataToBeUpdated.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds().ToString();
            _logger.LogInformation("This {MerchantName} order {GrainId} will be updated", inputOrderDto.MerchantName, grainId);

            var result = await orderGrain.UpdateOrderAsync(dataToBeUpdated);

            if (!result.Success)
                throw new FormatException($"Update order failed,{result.Message}");

            await _distributedEventBus.PublishAsync(_objectMapper.Map<OrderGrainDto, OrderEto>(result.Data));

            await _orderStatusProvider.AddOrderStatusInfoAsync(
                _objectMapper.Map<OrderGrainDto, OrderStatusInfoGrainDto>(result.Data));

            return new BasicOrderResult() { Success = true, Message = result.Message };
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning("Order update FAILED, {MerchantName}-{OrderId}-{ThirdPartOrderNo}", inputOrderDto?.MerchantName,
                inputOrderDto?.Id, inputOrderDto?.ThirdPartOrderNo);
            return new BasicOrderResult() { Message = $"Update order failed, {e.Message}." };
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Order update ERROR, {MerchantName}-{OrderId}-{ThirdPartOrderNo}", inputOrderDto?.MerchantName,
                inputOrderDto?.Id, inputOrderDto?.ThirdPartOrderNo);
            return new BasicOrderResult() { Message = "INTERNAL ERROR, please try again later." };
        }
    }
    
    
    public async Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input)
    {
        var userId = CurrentUser.GetId();
        var orderId = GuidGenerator.Create();
        var orderGrainData = _objectMapper.Map<CreateUserOrderDto, OrderGrainDto>(input);
        orderGrainData.UserId = userId;
        _logger.LogInformation("This third part {MerchantName} order {OrderId} will be created", input.MerchantName, orderId);
        orderGrainData.Status = OrderStatusType.Initialized.ToString();
        orderGrainData.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds().ToString();

        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);
        var result = await orderGrain.CreateUserOrderAsync(orderGrainData);
        if (!result.Success)
        {
            _logger.LogError("Create user order fail, {MerchantName} order id: {OrderId} user id: {UserId}", input.MerchantName, orderId,
                orderGrainData.UserId);
            return new OrderCreatedDto();
        }

        await _distributedEventBus.PublishAsync(_objectMapper.Map<OrderGrainDto, OrderEto>(result.Data));

        await _orderStatusProvider.AddOrderStatusInfoAsync(_objectMapper.Map<OrderGrainDto, OrderStatusInfoGrainDto>(result.Data));

        var resp = _objectMapper.Map<OrderGrainDto, OrderCreatedDto>(result.Data);
        resp.Success = true;
        return resp;
    }

    
    public async Task<OrdersDto> GetThirdPartOrdersAsync(GetUserOrdersDto input)
    {
        // var userId = input.UserId;
        var userId = CurrentUser.GetId();

        var orderList =
            await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(userId, input.SkipCount,
                input.MaxResultCount);
        return new OrdersDto
        {
            TotalRecordCount = orderList.Count,
            Data = orderList
        };
    }


    private OrderGrainDto MergeEsAndInput2GrainModel(OrderDto fromData, OrderDto toData)
    {
        var orderGrainData = _objectMapper.Map<OrderDto, OrderGrainDto>(fromData);
        var orderData = _objectMapper.Map<OrderDto, OrderGrainDto>(toData);
        foreach (var prop in typeof(OrderGrainDto).GetProperties())
        {
            // When the attribute in UpdateOrderData has been assigned, there is no need to overwrite it with the data in es
            if (prop.GetValue(orderGrainData) == null && prop.GetValue(orderData) != null)
            {
                prop.SetValue(orderGrainData, prop.GetValue(orderData));
            }
        }

        return orderGrainData;
    }

    
    public async Task ForwardTransactionAsync(TransactionDto input)
    {
        _logger.LogInformation("TransactionAsync start, {MerchantName} OrderId: {OrderId}", input.MerchantName,
            input.OrderId);
        if (!VerifyForwardTransactionInput(input))
        {
            _logger.LogWarning("Transaction input valid failed, orderId:{orderId}", input.OrderId);
            await _orderStatusProvider.UpdateOrderStatusAsync(new OrderStatusUpdateDto
            {
                OrderId = input.OrderId.ToString(),
                RawTransaction = input.RawTransaction,
                Status = OrderStatusType.Invalid,
                DicExt = new Dictionary<string, object>()
                {
                    ["reason"] = "Transaction input valid failed."
                }
            });
            throw new UserFriendlyException("Input validation failed.");
        }

        var transactionEto = _objectMapper.Map<TransactionDto, TransactionEto>(input);
        await _distributedEventBus.PublishAsync(transactionEto);
    }

    private bool VerifyForwardTransactionInput(TransactionDto input)
    {
        try
        {
            var validStr = EncryptionHelper.MD5Encrypt32(input.OrderId + input.RawTransaction);
            var publicKey = ByteArrayHelper.HexStringToByteArray(input.PublicKey);
            var signature = ByteArrayHelper.HexStringToByteArray(input.Signature);
            var data = Encoding.UTF8.GetBytes(validStr).ComputeHash();
            if (!CryptoHelper.VerifySignature(signature, data, publicKey))
                throw new UserFriendlyException("data validation failed");
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Input validation internal error");
            return false;
        }
    }
}
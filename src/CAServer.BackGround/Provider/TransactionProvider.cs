using AElf;
using AElf.Client.Dto;
using AElf.Kernel;
using AElf.Types;
using CAServer.BackGround.Dtos;
using CAServer.BackGround.Options;
using CAServer.Common;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.BackGround.Provider;

public interface ITransactionProvider
{
    Task HandleTransactionAsync(HandleTransactionDto transactionDto);
    Task HandleUnCompletedOrdersAsync();
    Task UpdateOrderStatusAsync(string orderId, string status);
}

public class TransactionProvider : ITransactionProvider, ISingletonDependency
{
    private readonly IContractProvider _contractProvider;
    private readonly ILogger<TransactionProvider> _logger;
    private readonly IAlchemyOrderAppService _alchemyOrderService;
    private readonly TransactionOptions _transactionOptions;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;

    public TransactionProvider(IContractProvider contractProvider, ILogger<TransactionProvider> logger,
        IAlchemyOrderAppService alchemyOrderService,
        IOptionsSnapshot<TransactionOptions> options,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IObjectMapper objectMapper,
        IDistributedEventBus distributedEventBus,
        IClusterClient clusterClient)
    {
        _contractProvider = contractProvider;
        _logger = logger;
        _alchemyOrderService = alchemyOrderService;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _transactionOptions = options.Value;
    }

    public async Task HandleTransactionAsync(HandleTransactionDto transactionDto)
    {
        var transaction =
            Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionDto.RawTransaction));
        var transactionResult = await QueryTransactionAsync(transactionDto.ChainId, transaction);

        // when to retry transaction, not existed-> retry  pending->wait for long  notinvaldidd->give up
        var times = 0;
        while (transactionResult.Status == TransactionState.NotExisted && times < _transactionOptions.RetryTime)
        {
            times++;
            await _contractProvider.SendRawTransaction(transactionDto.ChainId, transaction.ToByteArray().ToHex());
            transactionResult = await QueryTransactionAsync(transactionDto.ChainId, transaction);
        }

        var orderStatusInfo = new OrderStatusInfo
        {
            LastModifyTime = long.Parse(TimeStampHelper.GetTimeStampInMilliseconds()),
            Status = transactionResult.Status == TransactionState.Mined
                ? OrderStatusType.Transferred
                : OrderStatusType.TransferFailed
        };
        await UpdateOrderStatusAsync(transactionDto.OrderId.ToString(), orderStatusInfo.Status.ToString());

        if (transactionResult.Status != TransactionState.Mined)
        {
            _logger.LogWarning("Transaction handle fail, orderId:{}, transactionId:{transactionId}, status:{}",
                transactionDto.OrderId.ToString(), transaction.GetHash().ToHex(), transactionResult.Status);

            return;
        }

        // send to ach
        await SendToAlchemyAsync(transactionDto.MerchantName, transactionDto.OrderId.ToString(),
            transaction.ToByteArray().ToHex());
    }

    public async Task HandleUnCompletedOrdersAsync()
    {
        var orders = await _thirdPartOrderProvider.GetUnCompletedThirdPartOrdersAsync();
        _logger.LogInformation("{time}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        if (orders == null || orders.Count == 0) return;

        foreach (var order in orders)
        {
            try
            {
                // get status from ach.
                var orderInfo = await _alchemyOrderService.QueryAlchemyOrderInfo(order);
                if (orderInfo == null || string.IsNullOrWhiteSpace(orderInfo.OrderNo)) continue;

                var achOrderStatus = AlchemyHelper.GetOrderStatus(orderInfo.Status);
                await HandleUnCompletedOrderAsync(order, achOrderStatus);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Handle unCompleted order fail, orderId:{orderId}", order.Id);
            }
        }
    }

    public async Task UpdateOrderStatusAsync(string orderId, string status)
    {
        var orderData = await _thirdPartOrderProvider.GetThirdPartOrderAsync(orderId);

        if (string.IsNullOrEmpty(orderData.ThirdPartOrderNo) || string.IsNullOrEmpty(orderData.Id.ToString()) ||
            string.IsNullOrEmpty(orderData.TransDirect))
        {
            _logger.LogError("Order {OrderId} is not existed in storage.", orderId);
        }

        var grainId = ThirdPartHelper.GetOrderId(orderId);
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(grainId);
        var getGrainResult = await orderGrain.GetOrder();
        if (!getGrainResult.Success)
        {
            _logger.LogError("Order {OrderId} is not existed in storage.", orderId);
        }

        var grainDto = getGrainResult.Data;
        grainDto.Status = AlchemyHelper.GetOrderStatus(status);
        grainDto.LastModifyTime = TimeStampHelper.GetTimeStampInMilliseconds();

        var result = await orderGrain.UpdateOrderAsync(grainDto);

        if (!result.Success)
        {
            _logger.LogError("Update user order fail, third part order number: {orderId}",orderId);
        }

        await _distributedEventBus.PublishAsync(_objectMapper.Map<OrderGrainDto, OrderEto>(result.Data));

        await _thirdPartOrderProvider.AddOrderStatusInfoAsync(
            _objectMapper.Map<OrderGrainDto, OrderStatusInfoGrainDto>(result.Data));
    }

    private async Task<TransactionResultDto> QueryTransactionAsync(string chainId, Transaction transaction)
    {
        var transactionId = transaction.GetHash().ToHex();
        var transactionResult = await _contractProvider.GetTransactionResultAsync(chainId, transactionId);
        while (transactionResult.Status == TransactionState.Pending)
        {
            await Task.Delay(_transactionOptions.DelayTime);
            transactionResult = await _contractProvider.GetTransactionResultAsync(chainId, transactionId);
        }

        return transactionResult;
    }

    private async Task HandleUnCompletedOrderAsync(OrderDto order, string achOrderStatus)
    {
        if (order.Status == achOrderStatus) return;

        if (order.Status != OrderStatusType.Transferred.ToString() &&
            order.Status != OrderStatusType.StartTransfer.ToString() &&
            order.Status != OrderStatusType.Transferring.ToString() &&
            order.Status != achOrderStatus)
        {
            var statusInfoDto = _objectMapper.Map<OrderDto, OrderStatusInfoGrainDto>(order); // for debug
            await _thirdPartOrderProvider.AddOrderStatusInfoAsync(
                _objectMapper.Map<OrderDto, OrderStatusInfoGrainDto>(order));
        }

        var isOverInterval = long.Parse(TimeStampHelper.GetTimeStampInMilliseconds()) -
                             long.Parse(order.LastModifyTime) >
                             _transactionOptions.ResendTimeInterval * 1000;

        if (order.Status == OrderStatusType.Transferred.ToString() &&
            achOrderStatus == OrderStatusType.Created.ToString() && isOverInterval)
        {
            await SendToAlchemyAsync(order.MerchantName, order.Id.ToString(), order.TransactionId);
        }
    }

    private async Task SendToAlchemyAsync(string merchantName, string orderId, string txHash)
    {
        if (string.IsNullOrWhiteSpace(txHash)) return;

        await _alchemyOrderService.UpdateAlchemyTxHashAsync(new SendAlchemyTxHashDto
        {
            MerchantName = merchantName,
            OrderId = orderId,
            TxHash = txHash
        });
    }
}
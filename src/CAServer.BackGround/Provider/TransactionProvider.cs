using AElf;
using AElf.Client.Dto;
using AElf.Types;
using CAServer.BackGround.Dtos;
using CAServer.BackGround.Options;
using CAServer.Common;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.BackGround.Provider;

public interface ITransactionProvider
{
    Task HandleTransactionAsync(HandleTransactionDto transactionDto);
    Task HandleUnCompletedOrdersAsync();
}

public class TransactionProvider : ITransactionProvider, ISingletonDependency
{
    private readonly IContractProvider _contractProvider;
    private readonly ILogger<TransactionProvider> _logger;
    private readonly IAlchemyOrderAppService _alchemyOrderService;
    private readonly TransactionOptions _transactionOptions;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IOrderStatusProvider _orderStatusProvider;

    public TransactionProvider(IContractProvider contractProvider, ILogger<TransactionProvider> logger,
        IAlchemyOrderAppService alchemyOrderService,
        IOptionsSnapshot<TransactionOptions> options,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IObjectMapper objectMapper,
        IOrderStatusProvider orderStatusProvider)
    {
        _contractProvider = contractProvider;
        _logger = logger;
        _alchemyOrderService = alchemyOrderService;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _objectMapper = objectMapper;
        _orderStatusProvider = orderStatusProvider;
        _transactionOptions = options.Value;
    }

    public async Task HandleTransactionAsync(HandleTransactionDto transactionDto)
    {
        var transaction =
            Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionDto.RawTransaction));
        var transactionResult = await QueryTransactionAsync(transactionDto.ChainId, transaction);

        // not existed->retry  pending->wait  other->fail
        var times = 0;
        while (transactionResult.Status == TransactionState.NotExisted && times < _transactionOptions.RetryTime)
        {
            times++;
            await _contractProvider.SendRawTransaction(transactionDto.ChainId, transaction.ToByteArray().ToHex());
            transactionResult = await QueryTransactionAsync(transactionDto.ChainId, transaction);
        }

        var status = transactionResult.Status == TransactionState.Mined
            ? OrderStatusType.Transferred
            : OrderStatusType.TransferFailed;

        var dicExt = new Dictionary<string, object>();
        if (transactionResult.Status != TransactionState.Mined)
        {
            dicExt.Add("transactionError", transactionResult.Error);
        }

        var orderStatusUpdateDto = new OrderStatusUpdateDto()
        {
            OrderId = transactionDto.OrderId.ToString(),
            Status = status,
            RawTransaction = transactionDto.RawTransaction,
            DicExt = dicExt
        };

        await _orderStatusProvider.UpdateOrderStatusAsync(orderStatusUpdateDto);

        if (transactionResult.Status != TransactionState.Mined)
        {
            _logger.LogWarning(
                "Transaction handle fail, orderId:{orderId}, transactionId:{transactionId}, status:{status}",
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
        var count = orders?.Count ?? 0;
        _logger.LogInformation("Get uncompleted order from es, time: {time}, count: {count}",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), count);

        if (count == 0) return;

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
            order.Status != OrderStatusType.TransferFailed.ToString() &&
            order.Status != achOrderStatus)
        {
            await _orderStatusProvider.AddOrderStatusInfoAsync(
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
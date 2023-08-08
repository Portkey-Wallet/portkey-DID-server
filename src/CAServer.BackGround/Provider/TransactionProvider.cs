using AElf;
using AElf.Client.Dto;
using AElf.Types;
using CAServer.BackGround.Dtos;
using CAServer.BackGround.Options;
using CAServer.Common;
using CAServer.Commons;
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
    private readonly TransactionOptions _transactionOptions;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IOrderStatusProvider _orderStatusProvider;
    private readonly IThirdPartFactory _thirdPartFactory;

    public TransactionProvider(IContractProvider contractProvider, ILogger<TransactionProvider> logger,
        IOptionsSnapshot<TransactionOptions> options,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IObjectMapper objectMapper,
        IOrderStatusProvider orderStatusProvider, IThirdPartFactory thirdPartFactory)
    {
        _contractProvider = contractProvider;
        _logger = logger;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _objectMapper = objectMapper;
        _orderStatusProvider = orderStatusProvider;
        _thirdPartFactory = thirdPartFactory;
        _transactionOptions = options.Value;
    }

    public async Task HandleTransactionAsync(HandleTransactionDto transactionDto)
    {
        try
        {
            var transaction =
                Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionDto.RawTransaction));
            var transactionResult = await QueryTransactionAsync(transactionDto.ChainId, transaction);

            // not existed->retry  pending->wait  other->fail
            var times = 0;
            while (transactionResult.Status == TransactionState.NotExisted && times < _transactionOptions.RetryTime)
            {
                times++;
                await _contractProvider.SendRawTransactionAsync(transactionDto.ChainId,
                    transaction.ToByteArray().ToHex());

                await Task.Delay(_transactionOptions.DelayTime);
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
                transaction.GetHash().ToHex());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "handle transaction job fail, orderId:{orderId}, rawTransaction:{rawTransaction}",
                transactionDto.OrderId.ToString(), transactionDto.RawTransaction);
        }
    }

    public async Task HandleUnCompletedOrdersAsync()
    {
        var orders = await _thirdPartOrderProvider.GetUnCompletedThirdPartOrdersAsync();
        var count = orders?.Count ?? 0;
        _logger.LogInformation("Get uncompleted order from es, time: {Time}, count: {Count}",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), count);

        if (count == 0) return;

        foreach (var oldOrder in orders)
        {
            try
            {
                var orderProcessor = _thirdPartFactory.GetProcessor(oldOrder.MerchantName);
                // get status from ach.
                var orderInfo = await orderProcessor.QueryThirdOrderAsync(oldOrder);
                if (orderInfo == null || orderInfo.Id == Guid.Empty) continue;
                
                var achOrderStatus = AlchemyHelper.GetOrderStatus(orderInfo.Status).ToString();
                await HandleUnCompletedOrderAsync(oldOrder, achOrderStatus);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Handle unCompleted order fail, orderId:{OrderId}", oldOrder.Id);
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
            await _orderStatusProvider.UpdateOrderStatusAsync(new OrderStatusUpdateDto
            {
                OrderId = order.Id.ToString(),
                Order = order,
                Status = (OrderStatusType)Enum.Parse(typeof(OrderStatusType), achOrderStatus, true)
            });
            return;
        }

        if (order.Status == OrderStatusType.Transferred.ToString() &&
            achOrderStatus != OrderStatusType.Created.ToString())
        {
            await _orderStatusProvider.UpdateOrderStatusAsync(new OrderStatusUpdateDto
            {
                OrderId = order.Id.ToString(),
                Order = order,
                Status = (OrderStatusType)Enum.Parse(typeof(OrderStatusType), achOrderStatus, true)
            });
            return;
        }

        var isOverInterval = long.Parse(TimeHelper.GetTimeStampInMilliseconds().ToString()) -
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
        await _thirdPartFactory.GetProcessor(merchantName).UpdateTxHashAsync(new TransactionHashDto
        {
            MerchantName = merchantName,
            OrderId = orderId,
            TxHash = txHash
        });
    }
}
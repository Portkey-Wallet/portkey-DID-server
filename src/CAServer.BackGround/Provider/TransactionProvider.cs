using AElf;
using AElf.Client.Dto;
using AElf.Types;
using CAServer.BackGround.Dtos;
using CAServer.BackGround.Options;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;
    private readonly IOptionsMonitor<TransactionOptions> _transactionOptions;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;

    public TransactionProvider(IContractProvider contractProvider, ILogger<TransactionProvider> logger,
        IOptionsMonitor<TransactionOptions> options,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IOrderStatusProvider orderStatusProvider, IThirdPartOrderAppService thirdPartOrderAppService)
    {
        _contractProvider = contractProvider;
        _logger = logger;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _orderStatusProvider = orderStatusProvider;
        _thirdPartOrderAppService = thirdPartOrderAppService;
        _transactionOptions = options;
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
            while (transactionResult.Status == TransactionState.NotExisted &&
                   times < _transactionOptions.CurrentValue.RetryTime)
            {
                times++;
                await _contractProvider.SendRawTransactionAsync(transactionDto.ChainId,
                    transaction.ToByteArray().ToHex());

                await Task.Delay(_transactionOptions.CurrentValue.DelayTime);
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

            // send to thirdPart
            await _thirdPartOrderAppService.UpdateOffRampTxHashAsync(new TransactionHashDto
            {
                MerchantName = transactionDto.MerchantName,
                OrderId = transactionDto.OrderId.ToString(),
                TxHash = transaction.GetHash().ToHex()
            });
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
        _logger.LogInformation("Get uncompleted order from es, time: {time}, count: {count}",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), count);

        if (count == 0) return;

        foreach (var order in orders)
        {
            try
            {
                // get status from thirdPart.
                var thirdPartOrder = await _thirdPartOrderAppService.QueryThirdPartRampOrderAsync(order);
                AssertHelper.IsTrue(thirdPartOrder.Success, "Query order Fail");
                if (thirdPartOrder.Data == null || thirdPartOrder.Data.Id == Guid.Empty)
                {
                    _logger.LogWarning("Invalid order data :{Order}", JsonConvert.SerializeObject(thirdPartOrder));
                    continue;
                }

                await HandleUnCompletedOrderAsync(order, thirdPartOrder.Data.Status);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HandleAsync unCompleted order fail, orderId:{OrderId}", order.Id);
            }
        }
    }

    private async Task<TransactionResultDto> QueryTransactionAsync(string chainId, Transaction transaction)
    {
        var transactionId = transaction.GetHash().ToHex();
        var transactionResult = await _contractProvider.GetTransactionResultAsync(chainId, transactionId);
        while (transactionResult.Status == TransactionState.Pending)
        {
            await Task.Delay(_transactionOptions.CurrentValue.DelayTime);
            transactionResult = await _contractProvider.GetTransactionResultAsync(chainId, transactionId);
        }

        return transactionResult;
    }

    private async Task HandleUnCompletedOrderAsync(OrderDto order, string thirdPartOrderStatus)
    {
        if (order.Status == thirdPartOrderStatus)
        {
            _logger.LogDebug(
                "HandleUnCompletedOrderAsync status not changed orderId={OrderId}, status={Status}, newStatus={NewStatus}",
                order.Id, order.Status, thirdPartOrderStatus);
            return;
        }

        if (order.Status != OrderStatusType.Transferred.ToString() &&
            order.Status != OrderStatusType.StartTransfer.ToString() &&
            order.Status != OrderStatusType.Transferring.ToString() &&
            order.Status != OrderStatusType.TransferFailed.ToString() &&
            order.Status != thirdPartOrderStatus)
        {
            _logger.LogInformation(
                "HandleUnCompletedOrderAsync updateOrderStatus orderId={OrderId}, status={Status}, newStatus={NewStatus}",
                order.Id, order.Status, thirdPartOrderStatus);
            await _orderStatusProvider.UpdateOrderStatusAsync(new OrderStatusUpdateDto
            {
                OrderId = order.Id.ToString(),
                Order = order,
                Status = (OrderStatusType)Enum.Parse(typeof(OrderStatusType), thirdPartOrderStatus, true)
            });
            return;
        }

        if (order.Status == OrderStatusType.Transferred.ToString() &&
            thirdPartOrderStatus != OrderStatusType.Created.ToString())
        {
            _logger.LogInformation(
                "HandleUnCompletedOrderAsync fix transferred status orderId={OrderId}, status={Status}, newStatus={NewStatus}",
                order.Id, order.Status, thirdPartOrderStatus);
            await _orderStatusProvider.UpdateOrderStatusAsync(new OrderStatusUpdateDto
            {
                OrderId = order.Id.ToString(),
                Order = order,
                Status = (OrderStatusType)Enum.Parse(typeof(OrderStatusType), thirdPartOrderStatus, true)
            });
            return;
        }

        var isOverInterval = long.Parse(TimeHelper.GetTimeStampInMilliseconds().ToString()) -
                             long.Parse(order.LastModifyTime) >
                             _transactionOptions.CurrentValue.ResendTimeInterval * 1000;

        if (order.Status == OrderStatusType.Transferred.ToString() &&
            thirdPartOrderStatus == OrderStatusType.Created.ToString() && isOverInterval)
        {
            _logger.LogInformation(
                "HandleUnCompletedOrderAsync ResendTxHash orderId={OrderId}, status={Status}, newStatus={NewStatus}",
                order.Id, order.Status, thirdPartOrderStatus);
            await _thirdPartOrderAppService.UpdateOffRampTxHashAsync(new TransactionHashDto
            {
                MerchantName = order.MerchantName,
                OrderId = order.Id.ToString(),
                TxHash = order.TransactionId
            });
        }
    }
}
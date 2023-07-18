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
using Volo.Abp.DependencyInjection;

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

    public TransactionProvider(IContractProvider contractProvider, ILogger<TransactionProvider> logger,
        IAlchemyOrderAppService alchemyOrderService,
        IOptionsSnapshot<TransactionOptions> options,
        IThirdPartOrderProvider thirdPartOrderProvider)
    {
        _contractProvider = contractProvider;
        _logger = logger;
        _alchemyOrderService = alchemyOrderService;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _transactionOptions = options.Value;
    }

    public async Task HandleTransactionAsync(HandleTransactionDto transactionDto)
    {
        var transaction =
            Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionDto.RawTransaction));
        var transactionResult = await QueryTransactionAsync(transactionDto.ChainId, transaction);

        // when to retry transaction, not existed-> retry  pending->wait for long  notinvaldidd->give up
        var times = 0;
        while (transactionResult.Status != TransactionState.Mined && times < _transactionOptions.RetryTime)
        {
            times++;
            await _contractProvider.SendRawTransaction(transactionDto.ChainId, transaction.ToByteArray().ToHex());
            transactionResult = await QueryTransactionAsync(transactionDto.ChainId, transaction);
        }

        if (transactionResult.Status != TransactionState.Mined)
        {
            _logger.LogWarning("Transaction handle fail, transactionId:{transactionId}, status:{}",
                transaction.GetHash().ToHex(), transactionResult.Status);
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
                string achOrderStatus = "";
                // if status changed update
                var isOverInterval = long.Parse(TimeStampHelper.GetTimeStampInMilliseconds()) -
                                     long.Parse(order.LastModifyTime) >
                                     _transactionOptions.ResendTimeInterval * 1000;

                if (isOverInterval)
                {
                    _logger.LogError("over---======={orderId}", order.Id);
                }

                // time range
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

    private void StatusChange()
    {
    }

    private async Task HandleOrderAsync(OrderDto order, string achOrderStatus)
    {
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
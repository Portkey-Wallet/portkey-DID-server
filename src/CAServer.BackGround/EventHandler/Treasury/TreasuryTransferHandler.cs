using AElf;
using AElf.Client.Dto;
using AElf.Types;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Google.Protobuf;
using Medallion.Threading;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.BackGround.EventHandler.Treasury;

public class TreasuryTransferHandler : IDistributedEventHandler<TreasuryOrderEto>, ITransientDependency
{
    private readonly ILogger<TreasuryTransferHandler> _logger;
    private readonly IContractProvider _contractProvider;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly ITreasuryOrderProvider _treasuryOrderProvider;
    private readonly IAbpDistributedLock _distributedLock;

    private static readonly JsonSerializerSettings JsonSerializerSettings = JsonSettingsBuilder.New()
        .WithAElfTypesConverters()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .Build();

    public TreasuryTransferHandler(ILogger<TreasuryTransferHandler> logger, IContractProvider contractProvider,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions, ITreasuryOrderProvider treasuryOrderProvider,
        IAbpDistributedLock distributedLock)
    {
        _logger = logger;
        _contractProvider = contractProvider;
        _thirdPartOptions = thirdPartOptions;
        _treasuryOrderProvider = treasuryOrderProvider;
        _distributedLock = distributedLock;
    }


    private bool Match(TreasuryOrderEto eventData)
    {
        return eventData?.Data != null &&
               eventData.Data.TransferDirection == TransferDirectionType.TokenBuy.ToString() &&
               eventData.Data.Status == OrderStatusType.StartTransfer.ToString();
    }

    public async Task HandleEventAsync(TreasuryOrderEto eventData)
    {
        if (!Match(eventData)) return;

        var orderDto = eventData.Data;
        try
        {
            AssertHelper.NotEmpty(orderDto.RawTransaction, "RawTransaction empty");
            AssertHelper.NotEmpty(orderDto.TransactionId, "TransactionId empty");

            await using var locked =
                await _distributedLock.TryAcquireAsync("TreasuryTransfer:" + orderDto.TransactionId);
            if (locked == null)
            {
                _logger.LogWarning("Duplicated transaction event, orderId={OrderId}", orderDto.Id);
                return;
            }

            orderDto.Status = OrderStatusType.Transferring.ToString();
            orderDto = await _treasuryOrderProvider.DoSaveOrderAsync(orderDto);


            // send transaction to node
            var sendResult =
                await _contractProvider.SendRawTransactionAsync(CommonConstant.MainChainId, orderDto.RawTransaction);
            AssertHelper.NotNull(sendResult, "Send transfer result empty");

            // send transaction result
            var txResult = await WaitTransactionResultAsync(CommonConstant.MainChainId, orderDto.TransactionId);
            if (txResult == null) return;

            orderDto.Status = txResult.Status == TransactionState.Mined
                ? OrderStatusType.Transferred.ToString()
                : OrderStatusType.Transferring.ToString();

            var resExtensionBuilder = OrderStatusExtensionBuilder.Create()
                .Add(ExtensionKey.TxStatus, txResult.Status)
                .Add(ExtensionKey.TxBlockHeight, txResult.BlockNumber.ToString());

            if (txResult.Status != TransactionState.Mined)
                resExtensionBuilder.Add(ExtensionKey.TxResult,
                    JsonConvert.SerializeObject(txResult, JsonSerializerSettings));

            await _treasuryOrderProvider.DoSaveOrderAsync(orderDto, resExtensionBuilder.Build());
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning("TreasuryTransferHandler failed: {Message}, orderId={OrderId}, status={Status}",
                e.Message, orderDto.Id, orderDto.Status);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TreasuryTransferHandler error, orderId={OrderId}, status={Status}", orderDto.Id,
                orderDto.Status);
        }
    }


    private async Task<TransactionResultDto?> WaitTransactionResultAsync(string chainId, string transactionId)
    {
        var waitingStatus = new List<string> { TransactionState.NotExisted, TransactionState.Pending };
        var maxWaitMillis = _thirdPartOptions.CurrentValue.Timer.TransactionWaitTimeoutSeconds * 1000;
        var delayMillis = _thirdPartOptions.CurrentValue.Timer.TransactionWaitDelaySeconds * 1000;
        TransactionResultDto? rawTxResult = null;
        using var cts = new CancellationTokenSource(maxWaitMillis);
        try
        {
            while (!cts.IsCancellationRequested && (rawTxResult == null || waitingStatus.Contains(rawTxResult.Status)))
            {
                // delay some times
                await Task.Delay(delayMillis, cts.Token);

                rawTxResult = await _contractProvider.GetTransactionResultAsync(chainId, transactionId);
                _logger.LogDebug(
                    "WaitTransactionResultAsync chainId={ChainId}, transactionId={TransactionId}, status={Status}",
                    chainId,
                    transactionId, rawTxResult.Status);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Timed out waiting for transactionId {TransactionId} result", transactionId);
        }

        return rawTxResult;
    }
}
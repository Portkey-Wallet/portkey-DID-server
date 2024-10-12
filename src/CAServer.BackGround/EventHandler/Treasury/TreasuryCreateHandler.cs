using AElf;
using AElf.ExceptionHandler;
using AElf.Types;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Monitor.Interceptor;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.BackGround.EventHandler.Treasury;

public class TreasuryCreateHandler : IDistributedEventHandler<TreasuryOrderEto>, ITransientDependency
{
    private readonly ILogger<TreasuryCreateHandler> _logger;
    private readonly IContractProvider _contractProvider;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly ITreasuryOrderProvider _treasuryOrderProvider;
    private readonly IAbpDistributedLock _distributedLock;

    public TreasuryCreateHandler(ILogger<TreasuryCreateHandler> logger, IContractProvider contractProvider,
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
               eventData.Data.Status == OrderStatusType.Created.ToString();
    }

    [ExceptionHandler(typeof(Exception),
        Message = "TreasuryCreateHandler exist error",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(TreasuryOrderEto eventData)
    {
        if (!Match(eventData)) return;

        var orderDto = eventData.Data;
        _logger.LogDebug("TreasuryCreateHandler start, {OrderId}-{Version}-{Status}", orderDto.Id, orderDto.Version,
            orderDto.Status);

        await using var locked =
            await _distributedLock.TryAcquireAsync("TreasuryTxCreate:" + orderDto.TransactionId);
        if (locked == null)
        {
            _logger.LogWarning("Duplicated create event, orderId={OrderId}", orderDto.Id);
            return;
        }


        AssertHelper.IsTrue(orderDto.TransactionId.IsNullOrEmpty(), "Transaction id exists");
        AssertHelper.IsTrue(orderDto.RawTransaction.IsNullOrEmpty(), "Raw transaction empty");

        var settlementAddressKey = string.Join(CommonConstant.Underline, orderDto.ThirdPartName, orderDto.Crypto);
        AssertHelper.IsTrue(
            _thirdPartOptions.CurrentValue.TreasuryOptions.SettlementPublicKey.TryGetValue(settlementAddressKey,
                out var senderPublicKey), "Settlement sender not exists {}", settlementAddressKey);

        var senderAddress = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(senderPublicKey));
        AssertHelper.NotNull(senderAddress, "Invalid settlement sender {}", senderAddress.ToBase58());

        var exchange = orderDto.TokenExchanges
            .Where(ex => ex.FromSymbol == orderDto.Crypto)
            .FirstOrDefault(ex => ex.ToSymbol == CommonConstant.USDT);
        AssertHelper.NotNull(exchange, "Exchange in order not found {}-{}", orderDto.Crypto, CommonConstant.USDT);

        var feeInUsdt = orderDto.FeeInfo
            .Select(fee => fee.Amount.SafeToDecimal() * fee.SymbolPriceInUsdt.SafeToDecimal()).Sum();
        var feeInCrypto = feeInUsdt / exchange!.Exchange;
        var transferAmount = (orderDto.CryptoAmount - feeInCrypto) * (decimal)Math.Pow(10, orderDto.CryptoDecimals);
        var (txId, tx) = await _contractProvider.GenerateTransferTransactionAsync(orderDto.Crypto,
            transferAmount.ToString(0, DecimalHelper.RoundingOption.Floor), orderDto.ToAddress,
            CommonConstant.MainChainId, senderPublicKey);

        // fill transaction data
        orderDto.TransactionId = txId;
        orderDto.RawTransaction = tx.ToByteArray().ToHex();
        orderDto.TransactionTime = DateTime.UtcNow.ToUtcMilliSeconds();
        orderDto.Status = OrderStatusType.StartTransfer.ToString();

        await _treasuryOrderProvider.DoSaveOrderAsync(orderDto, OrderStatusExtensionBuilder.Create()
            .Add(ExtensionKey.TxHash, txId)
            .Add(ExtensionKey.Transaction, orderDto.RawTransaction)
            .Build());
    }
}
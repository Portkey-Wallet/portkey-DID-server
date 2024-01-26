using AElf;
using AElf.Types;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.BackGround.EventHandler.Treasury;

public class TreasuryCreateHandler : IDistributedEventHandler<TreasuryOrderEto>, ITransientDependency
{
    private readonly ILogger<TreasuryCreateHandler> _logger;
    private readonly IContractProvider _contractProvider;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly ITreasuryOrderProvider _treasuryOrderProvider;

    public TreasuryCreateHandler(ILogger<TreasuryCreateHandler> logger, IContractProvider contractProvider,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions, ITreasuryOrderProvider treasuryOrderProvider)
    {
        _logger = logger;
        _contractProvider = contractProvider;
        _thirdPartOptions = thirdPartOptions;
        _treasuryOrderProvider = treasuryOrderProvider;
    }


    private bool Match(TreasuryOrderEto eventData)
    {
        return eventData.Data.TransferDirection == TransferDirectionType.TokenBuy.ToString() 
               && eventData.Data.Status == OrderStatusType.Created.ToString();
    }

    public async Task HandleEventAsync(TreasuryOrderEto eventData)
    {
        if (!Match(eventData)) return;

        var orderDto = eventData.Data;
        _logger.LogDebug("TreasuryCreateHandler start, {OrderId}-{Version}-{Status}", orderDto.Id, orderDto.Version, orderDto.Status);
        try
        {
            AssertHelper.IsTrue(orderDto.TransactionId.IsNullOrEmpty(), "Transaction id exists");
            AssertHelper.IsTrue(orderDto.RawTransaction.IsNullOrEmpty(), "Raw transaction empty");

            var settlementAddressKey = string.Join(CommonConstant.Underline, orderDto.ThirdPartName, orderDto.Crypto);
            AssertHelper.IsTrue(
                _thirdPartOptions.CurrentValue.TreasuryOptions.SettlementPublicKey.TryGetValue(settlementAddressKey,
                    out var senderPublicKey), "Settlement sender not exists {}", settlementAddressKey);
            var senderAddress = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(senderPublicKey));
            AssertHelper.NotNull(senderAddress, "Invalid settlement sender {}", senderAddress.ToBase58());

            var transferAmount = orderDto.CryptoAmount * (decimal)Math.Pow(10, orderDto.CryptoDecimals);
            var (txId, tx) = await _contractProvider.GenerateTransferTransactionAsync(orderDto.Crypto,
                transferAmount.ToString(0, DecimalHelper.RoundingOption.Floor), orderDto.ToAddress,
                CommonConstant.MainChainId, senderPublicKey);

            orderDto.TransactionId = txId;
            orderDto.RawTransaction = tx.ToByteArray().ToHex();
            orderDto.TransactionTime = DateTime.UtcNow.ToUtcMilliSeconds();
            orderDto.Status = OrderStatusType.StartTransfer.ToString();

            await _treasuryOrderProvider.DoSaveOrder(orderDto, OrderStatusExtensionBuilder.Create()
                .Add(ExtensionKey.TxHash, txId)
                .Add(ExtensionKey.Transaction, orderDto.RawTransaction)
                .Build());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TreasuryCreateHandler error, orderId={OrderId}, status={Status}", orderDto.Id,
                orderDto.Status);
        }
    }
}
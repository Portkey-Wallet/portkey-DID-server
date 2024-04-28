using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;
using CAServer.BackGround.Dtos;
using CAServer.BackGround.Options;
using CAServer.BackGround.Provider;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Hangfire;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.BackGround.EventHandler;

public class TransactionHandler : IDistributedEventHandler<TransactionEto>, ITransientDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TransactionHandler> _logger;
    private readonly IContractProvider _contractProvider;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IActivityProvider _activityProvider;
    private readonly TransactionOptions _transactionOptions;
    private readonly IOrderStatusProvider _orderStatusProvider;
    private readonly IOptionsMonitor<ChainOptions> _chainOptions;

    public TransactionHandler(
        IObjectMapper objectMapper,
        ILogger<TransactionHandler> logger,
        IContractProvider contractProvider,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IActivityProvider activityProvider,
        IOptionsSnapshot<TransactionOptions> options,
        IOrderStatusProvider orderStatusProvider, IOptionsMonitor<ChainOptions> contractOptions)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _contractProvider = contractProvider;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _activityProvider = activityProvider;
        _transactionOptions = options.Value;
        _orderStatusProvider = orderStatusProvider;
        _chainOptions = contractOptions;
    }

    public async Task HandleEventAsync(TransactionEto eventData)
    {
        try
        {
            if (eventData.OrderId == Guid.Empty)
            {
                _logger.LogWarning("TransactionHandler receive empty eto: orderId:{OrderId}, rawTransaction:{RawTransaction}, publicKey:{PublicKey}",
                    eventData.OrderId, eventData.RawTransaction, eventData.PublicKey);
                return;
            } 
            var transaction =
                Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(eventData.RawTransaction));
            var order = await _thirdPartOrderProvider.GetThirdPartOrderIndexAsync(eventData.OrderId.ToString());

            if (order.Status != OrderStatusType.Created.ToString())
            {
                _logger.LogWarning("Order status is NOT Create, orderId:{OrderId}", eventData.OrderId);
                return;
            }

            await ValidTransactionAsync(transaction, eventData.PublicKey, order);
            order.TransactionId = transaction.GetHash().ToHex();
            order.TxTime = DateTime.UtcNow.ToUtcMilliSeconds().ToString();
            order.Status = OrderStatusType.StartTransfer.ToString();

            await _orderStatusProvider.UpdateOrderStatusAsync(new OrderStatusUpdateDto
            {
                Order = _objectMapper.Map<RampOrderIndex, OrderDto>(order),
                Status = OrderStatusType.StartTransfer,
                RawTransaction = eventData.RawTransaction,
                DicExt = new Dictionary<string, object>
                {
                    ["TransactionId"] = order.TransactionId,
                    ["RawTransaction"] = eventData.RawTransaction
                }
            });

            var chainId = _transactionOptions.SendToChainId;
            await _contractProvider.SendRawTransactionAsync(chainId, eventData.RawTransaction);

            order.Status = OrderStatusType.Transferring.ToString();
            await _orderStatusProvider.UpdateOrderStatusAsync(new OrderStatusUpdateDto
            {
                Order = _objectMapper.Map<RampOrderIndex, OrderDto>(order),
                Status = OrderStatusType.Transferring,
                RawTransaction = eventData.RawTransaction
            });

            var transactionDto = _objectMapper.Map<TransactionEto, HandleTransactionDto>(eventData);
            transactionDto.ChainId = chainId;

            _logger.LogDebug(
                "HandleAsync transaction: orderId:{OrderId}, rawTransaction:{RawTransaction}, publicKey:{PublicKey}",
                eventData.OrderId, eventData.RawTransaction, eventData.PublicKey);
            BackgroundJob.Schedule<ITransactionProvider>(provider =>
                provider.HandleTransactionAsync(transactionDto), TimeSpan.FromSeconds(_transactionOptions.DelayTime));
        }
        catch (Exception e)
        {
            // add alarm.
            _logger.LogError(e,
                "HandleAsync transaction fail: orderId:{orderId}, rawTransaction:{rawTransaction}, publicKey:{publicKey}",
                eventData.OrderId, eventData.RawTransaction, eventData.PublicKey);

            await _orderStatusProvider.UpdateOrderStatusAsync(new OrderStatusUpdateDto
            {
                OrderId = eventData.OrderId.ToString(),
                Status = OrderStatusType.Invalid,
                RawTransaction = eventData.RawTransaction,
                DicExt = new Dictionary<string, object>() { ["reason"] = e.Message }
            });
        }
    }

    private async Task ValidTransactionAsync(Transaction transaction, string publicKey, RampOrderIndex order)
    {
        if (!VerifyHelper.VerifySignature(transaction, publicKey))
            throw new UserFriendlyException("RawTransaction validation failed");

        var chainExists =
            _chainOptions.CurrentValue.ChainInfos.TryGetValue(CommonConstant.MainChainId, out var chainInfo);
        if (!chainExists || chainInfo == null)
            throw new UserFriendlyException("Chain info missing");
        
        if (chainInfo.ContractAddress != transaction.To.ToBase58())
            throw new UserFriendlyException("Invalid transaction to address");

        if (order == null)
            throw new UserFriendlyException("Order not exists");

        if (!order.TransactionId.IsNullOrWhiteSpace())
            throw new UserFriendlyException("TransactionId exists");

        var forwardCallDto =
            ManagerForwardCallDto<TransferInput>.Decode(transaction);

        TransferInput? transferInput;
        if (forwardCallDto == null
            || forwardCallDto.MethodName != "Transfer"
            || (transferInput = forwardCallDto.ForwardTransactionArgs?.Value as TransferInput) == null)
            throw new UserFriendlyException("NOT Transfer-ManagerForwardCall transaction");
        
        if (chainInfo.TokenContractAddress != forwardCallDto.ContractAddress.ToBase58())
            throw new UserFriendlyException("Invalid forward contract address");
        
        if (order.Address.IsNullOrEmpty())
            throw new UserFriendlyException("Order address not exists");

        if (transferInput.To.ToBase58() != order.Address)
            throw new UserFriendlyException("Transfer address not match");

        if (transferInput.Symbol != order.Crypto)
            throw new UserFriendlyException("Transfer symbol not match");

        var decimalsList = await _activityProvider.GetTokenDecimalsAsync(transferInput.Symbol);
        if (decimalsList == null || decimalsList.TokenInfo.IsNullOrEmpty())
            throw new UserFriendlyException("Decimal of Symbol [{}] NOT found", transferInput.Symbol);
        var decimals = decimalsList.TokenInfo.First().Decimals;

        var amount = transferInput.Amount / Math.Pow(10, decimals);
        if (amount - double.Parse(order.CryptoAmount) != 0)
            throw new UserFriendlyException("Transfer amount NOT match");
    }
}
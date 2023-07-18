using AElf;
using AElf.Contracts.MultiToken;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.BackGround.Dtos;
using CAServer.CAActivity.Provider;
using CAServer.BackGround.Options;
using CAServer.BackGround.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using Elasticsearch.Net;
using Google.Protobuf;
using Hangfire;
using Nest;
using Volo.Abp.BackgroundJobs;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.BackGround.EventHandler;

public class TransactionHandler : IDistributedEventHandler<TransactionEto>, ITransientDependency
{
    private readonly INESTRepository<RampOrderIndex, Guid> _orderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TransactionHandler> _logger;
    private readonly ITransactionProvider _transactionProvider;
    private readonly IContractProvider _contractProvider;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IActivityProvider _activityProvider;
    private readonly TransactionOptions _transactionOptions;

    public TransactionHandler(INESTRepository<RampOrderIndex, Guid> orderRepository,
        IObjectMapper objectMapper,
        ILogger<TransactionHandler> logger,
        IContractProvider contractProvider,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IActivityProvider activityProvider,
        ITransactionProvider transactionProvider,
        IOptionsSnapshot<TransactionOptions> options)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _contractProvider = contractProvider;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _activityProvider = activityProvider;
        _transactionProvider = transactionProvider;
        _transactionOptions = options.Value;
    }

    public async Task HandleEventAsync(TransactionEto eventData)
    {
        try
        {
            var transaction =
                Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(eventData.RawTransaction));
            //var order = await _thirdPartOrderProvider.GetThirdPartOrderIndexAsync(eventData.OrderId.ToString());
            var transactionId = transaction.GetHash().ToHex();

            var order = new RampOrderIndex();
           // await ValidTransactionAsync(transaction, eventData.PublicKey, order);

            if (order.TransactionId.IsNullOrEmpty())
            {
                order.TransactionId = transactionId;
                order.Status = OrderStatusType.StartTransfer.ToString();
                //await _orderRepository.UpdateAsync(order);
            }

            var chainId = _transactionOptions.SendToChainId;
            //send transaction
            await _contractProvider.SendRawTransaction(chainId, eventData.RawTransaction);
            order.Status = OrderStatusType.Transferring.ToString();
           // await _orderRepository.UpdateAsync(order);

            var transactionDto = _objectMapper.Map<TransactionEto, HandleTransactionDto>(eventData);
            transactionDto.ChainId = chainId;

            // BackgroundJob.Schedule<ITransactionProvider>(provider =>
            //     provider.HandleTransactionAsync(transactionDto), TimeSpan.FromSeconds(_transactionOptions.DelayTime));
        }
        catch (Exception e)
        {
            // add alarm.
            _logger.LogError(e, "Handle transaction fail: {message}", JsonConvert.SerializeObject(eventData));
        }
    }

    private async Task ValidTransactionAsync(Transaction transaction, string publicKey, RampOrderIndex rampOrder)
    {
        if (!VerifyHelper.VerifySignature(transaction, publicKey))
            throw new UserFriendlyException("RawTransaction validation failed");

        if (rampOrder == null)
            throw new UserFriendlyException("Order not exists");

        if (rampOrder.Status != OrderStatusType.Created.ToString())
            throw new UserFriendlyException("Order status is NOT Create");

        if (!rampOrder.TransactionId.IsNullOrWhiteSpace())
            throw new UserFriendlyException("TransactionId exists");

        var forwardCallDto =
            ManagerForwardCallDto<TransferInput>.Decode(transaction);

        TransferInput? transferInput;
        if (forwardCallDto == null
            || forwardCallDto.MethodName != "Transfer"
            || (transferInput = forwardCallDto.Args?.Value as TransferInput) == null)
            throw new UserFriendlyException("NOT Transfer-ManagerForwardCall transaction");

        if (rampOrder.Address.IsNullOrEmpty())
            throw new UserFriendlyException("Order address not exists");

        if (transferInput.To.ToBase58() != rampOrder.Address)
            throw new UserFriendlyException("Transfer address not match");

        if (transferInput.Symbol != rampOrder.Crypto)
            throw new UserFriendlyException("Transfer symbol not match");

        var decimalsList = await _activityProvider.GetTokenDecimalsAsync(transferInput.Symbol);
        if (decimalsList == null || decimalsList.TokenInfo.IsNullOrEmpty())
            throw new UserFriendlyException("Decimal of Symbol [{}] NOT found", transferInput.Symbol);
        var decimals = decimalsList.TokenInfo.First().Decimals;

        var amount = transferInput.Amount / Math.Pow(10, decimals);
        if (amount - double.Parse(rampOrder.CryptoQuantity) != 0)
            throw new UserFriendlyException("Transfer amount NOT match");
    }
}
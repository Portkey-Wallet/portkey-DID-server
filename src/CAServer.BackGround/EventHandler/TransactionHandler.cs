using AElf;
using AElf.Contracts.MultiToken;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.CAActivity.Provider;
using CAServer.BackGround.Job;
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
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.BackGround.EventHandler;

public class TransactionHandler : IDistributedEventHandler<TransactionEto>, ITransientDependency
{
    private readonly INESTRepository<OrderIndex, Guid> _orderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TransactionHandler> _logger;
    private readonly ITransactionProvider _transactionProvider;
    private readonly IContractProvider _contractProvider;
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IActivityProvider _activityProvider;

    public TransactionHandler(INESTRepository<OrderIndex, Guid> orderRepository,
        IObjectMapper objectMapper,
        ILogger<TransactionHandler> logger,
        IContractProvider contractProvider,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IActivityProvider activityProvider,
        IBackgroundJobManager backgroundJobManager,
        ITransactionProvider transactionProvider)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _contractProvider = contractProvider;
        _backgroundJobManager = backgroundJobManager;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _activityProvider = activityProvider;
        _transactionProvider = transactionProvider;
    }

    public async Task HandleEventAsync(TransactionEto eventData)
    {
        var transaction = Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(eventData.RawTransaction));
        var order = await _thirdPartOrderProvider.GetThirdPartOrderIndexAsync(eventData.OrderId.ToString());
        var transactionId = HashHelper.ComputeFrom(ByteArrayHelper.HexStringToByteArray(eventData.RawTransaction));

        try
        {
            if (!VerifyHelper.VerifySignature(transaction, eventData.PublicKey))
                throw new UserFriendlyException("RawTransaction validation failed");

            if (order == null)
                throw new UserFriendlyException("Order not exists");

            // if (order.Status != OrderStatusType.Created.ToString())
            // throw new UserFriendlyException("Order status is NOT Create");

            if (order.TransactionId != null && order.TransactionId != transactionId.ToHex())
                throw new UserFriendlyException("TransactionId exists");

            var forwardCallDto =
                ManagerForwardCallDto<TransferInput>.Decode(transaction);

            TransferInput? transferInput;
            if (forwardCallDto == null
                || forwardCallDto.MethodName != "Transfer"
                || (transferInput = forwardCallDto.Args?.Value as TransferInput) == null)
                throw new UserFriendlyException("NOT Transfer-ManagerForwardCall transaction");

            if (order.Address.IsNullOrEmpty())
                throw new UserFriendlyException("Order address not exists");

            if (transferInput.To.ToBase58() != order.Address)
                throw new UserFriendlyException("Transfer address not match");

            if (transferInput.Symbol != order.Crypto)
                throw new UserFriendlyException("Transfer symbol not match");

            // var decimalsList = await _activityProvider.GetTokenDecimalsAsync(transferInput.Symbol);
            // if (decimalsList == null || decimalsList.TokenInfo.IsNullOrEmpty())
            //     throw new UserFriendlyException("Decimal of Symbol [{}] NOT found", transferInput.Symbol);
            var decimals = 8; //decimalsList.TokenInfo.First().Decimals;

            var amount = transferInput.Amount / Math.Pow(10, decimals);
            if (amount - double.Parse(order.CryptoQuantity) != 0)
                throw new UserFriendlyException("Transfer amount NOT match");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "HandleEventAsync failed, userId={}, orderId={}, transactionId={}",
                eventData.UserId, eventData.OrderId, transactionId);
            return;
        }

        if (order.TransactionId.IsNullOrEmpty())
        {
            order.TransactionId = transactionId.ToHex();
            order.Status = OrderStatusType.StartTransfer.ToString();
            await _orderRepository.UpdateAsync(order);
        }


        order.TransactionId = transaction.GetHash().ToHex();
        order.Status = OrderStatusType.StartTransfer.ToString();

        await _orderRepository.UpdateAsync(order);
        var chainId = string.Empty;
        //send transaction
        await _contractProvider.SendRawTransaction(chainId, eventData.RawTransaction);
        order.Status = OrderStatusType.Transferring.ToString();
        await _orderRepository.UpdateAsync(order);

        var raw = transaction.ToByteArray().ToHex();
        BackgroundJob.Schedule<ITransactionProvider>(provider =>
            provider.HandleTransactionAsync(chainId, raw), TimeSpan.FromSeconds(2));
    }

    private async Task<OrderIndex> UpdateOrderAsync(Guid orderId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OrderIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(orderId)));

        QueryContainer Filter(QueryContainerDescriptor<OrderIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _orderRepository.GetAsync(Filter);
    }

    private async Task<OrderIndex> GetOrderAsync(Guid orderId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OrderIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(orderId)));

        QueryContainer Filter(QueryContainerDescriptor<OrderIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _orderRepository.GetAsync(Filter);
    }
}
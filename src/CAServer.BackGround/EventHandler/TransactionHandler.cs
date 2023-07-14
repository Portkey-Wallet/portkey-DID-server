using AElf;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using Elasticsearch.Net;
using Google.Protobuf;
using Hangfire;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.BackGround.EventHandler;

public class TransactionHandler : IDistributedEventHandler<TransactionEto>, ITransientDependency
{
    private readonly INESTRepository<OrderIndex, Guid> _orderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TransactionHandler> _logger;
    private readonly IContractProvider _contractProvider;

    public TransactionHandler(INESTRepository<OrderIndex, Guid> orderRepository, IObjectMapper objectMapper,
        ILogger<TransactionHandler> logger,
        IContractProvider contractProvider)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _contractProvider = contractProvider;
    }

    public async Task HandleEventAsync(TransactionEto eventData)
    {
        var transactionId =
            HashHelper.ComputeFrom(ByteArrayHelper.HexStringToByteArray(eventData.RawTransaction));

        var transaction = Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(eventData.RawTransaction));
        if (!VerifyHelper.VerifySignature(transaction, eventData.PublicKey))
        {
            _logger.LogError("Verify signature fail, orderId:{orderId}", eventData.OrderId);
        }

        //var order = await GetOrderAsync(eventData.OrderId);

        var order = new OrderIndex();

        // todo  verify userId crypto and amount and methodName=ManagerForwardCall


        string paramsss = ByteString.CopyFrom(transaction.Params.ToByteArray()).ToHex();
        var transaction2 = Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(paramsss));
        //if(order.Crypto==transaction.s)

        var aaa = transactionId.ToHex();
        order.TransactionId = transaction.GetHash().ToHex();
        order.Status = OrderStatusType.StartTransfer.ToString();
        // todo send transaction

        // await _orderRepository.UpdateAsync(order);

        // todo put into hangfire search tx status
        var optput = await _contractProvider.SendRawTransaction("AELF", eventData.RawTransaction);
        //optput.TransactionId
        BackgroundJob.Schedule(() => Console.WriteLine("Delayed!"), TimeSpan.FromSeconds(10));
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
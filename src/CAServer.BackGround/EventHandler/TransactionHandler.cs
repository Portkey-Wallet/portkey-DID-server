using AElf;
using AElf.Client.MultiToken;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Google.Protobuf;
using Hangfire;
using Nest;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

namespace CAServer.BackGround.EventHandler;

public class TransactionHandler : IDistributedEventHandler<TransactionEto>, ITransientDependency
{
    private readonly INESTRepository<OrderIndex, Guid> _orderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TransactionHandler> _logger;
    private readonly IContractProvider _contractProvider;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;

    public TransactionHandler(INESTRepository<OrderIndex, Guid> orderRepository, IObjectMapper objectMapper,
        ILogger<TransactionHandler> logger,
        IContractProvider contractProvider, IThirdPartOrderProvider thirdPartOrderProvider)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _contractProvider = contractProvider;
        _thirdPartOrderProvider = thirdPartOrderProvider;
    }

    public async Task HandleEventAsync(TransactionEto eventData)
    {
        var order = await _thirdPartOrderProvider.GetThirdPartOrderIndexAsync(eventData.OrderId.ToString());
        var transactionId = HashHelper.ComputeFrom(ByteArrayHelper.HexStringToByteArray(eventData.RawTransaction));
        
        try
        {
            if (order == null)
                throw new UserFriendlyException("Order not exists");
            
            // if (order.Status != OrderStatusType.Created.ToString())
                // throw new UserFriendlyException("Order status is NOT Create");
            
            if (order.TransactionId != null && order.TransactionId != transactionId.ToHex())
                throw new UserFriendlyException("TransactionId exists");

            var forwardCallDto = ManagerForwardCallDto<AElf.Contracts.MultiToken.TransferInput>.Decode(eventData.Transaction);
        
            TransferInput? transferInput;
            if (forwardCallDto == null 
                || forwardCallDto.MethodName != "Transfer" 
                || (transferInput = forwardCallDto.Args?.Value as AElf.Contracts.MultiToken.TransferInput) == null)
                throw new UserFriendlyException("NOT Transfer-ManagerForwardCall transaction");

            if (order.Address.IsNullOrEmpty())
                throw new UserFriendlyException("Order address not exists");

            if (transferInput.To.ToBase58() != order.Address)
                throw new UserFriendlyException("Transfer address NOT match");
        
            if (transferInput.Amount - double.Parse(order.CryptoQuantity) != 0)
                throw new UserFriendlyException("Transfer amount NOT match");

            if (transferInput.Symbol == order.Crypto) 
                throw new UserFriendlyException("Transfer symbol NOT match");

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
        
        
        // todo send transaction

        // todo put into hangfire search tx status
        var optput = await _contractProvider.SendRawTransaction("AELF", eventData.RawTransaction);
        //optput.TransactionId
        BackgroundJob.Schedule(() => Console.WriteLine("Delayed!"), TimeSpan.FromSeconds(10));
        // BackgroundJob.Enqueue<IContractProvider>(async  x=> await x.SendRawTransaction());
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
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
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
    private readonly IContractProvider _contractProvider;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IActivityProvider _activityProvider;

    public TransactionHandler(INESTRepository<OrderIndex, Guid> orderRepository, 
        IObjectMapper objectMapper,
        ILogger<TransactionHandler> logger,
        IContractProvider contractProvider, 
        IThirdPartOrderProvider thirdPartOrderProvider, 
        IActivityProvider activityProvider)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _contractProvider = contractProvider;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _activityProvider = activityProvider;
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
                throw new UserFriendlyException("Transfer address NOT match");
            
            if (transferInput.Symbol != order.Crypto)
                throw new UserFriendlyException("Transfer symbol NOT match");

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


        // todo send transaction

        // todo put into hangfire search tx status
        // var optput = await _contractProvider.SendRawTransaction("AELF", eventData.RawTransaction);
        // optput.TransactionId
        // BackgroundJob.Schedule(() => Console.WriteLine("Delayed!"), TimeSpan.FromSeconds(10));
        // BackgroundJob.Enqueue<IContractProvider>(async  x=> await x.SendRawTransaction());
    }

}
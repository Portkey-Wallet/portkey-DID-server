using AElf;
using AElf.Client.Dto;
using AElf.Types;
using CAServer.BackGround.Dtos;
using CAServer.BackGround.Options;
using CAServer.Common;
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

        // when to retry transaction
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
        await _alchemyOrderService.UpdateAlchemyTxHashAsync(new SendAlchemyTxHashDto()
        {
            MerchantName = transactionDto.MerchantName,
            OrderId = transactionDto.OrderId.ToString(),
            TxHash = transaction.ToByteArray().ToHex()
        });
    }

    public async Task HandleUnCompletedOrdersAsync()
    {
        var orders = await _thirdPartOrderProvider.GetUnCompletedThirdPartOrdersAsync();
        if (orders == null || orders.Count == 0) return;
        
        foreach (var order in orders)
        {
            // get status from ach.
            
            // if status changed update
            
            // when to retry???
        }

        _logger.LogError("========{time}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
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
}
using AElf.Client.Dto;
using CAServer.BackGround.Job;
using CAServer.BackGround.Options;
using CAServer.Common;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.BackGround.Provider;

public interface ITransactionProvider
{
    Task HandleTransactionAsync(string chainId, string rawTransaction);
}

public class TransactionProvider : ITransactionProvider, ISingletonDependency
{
    private readonly IContractProvider _contractProvider;
    private readonly ILogger<TransactionJob> _logger;
    private readonly IAlchemyOrderAppService _alchemyOrderService;
    private readonly TransactionOptions _transactionOptions;

    public TransactionProvider(IContractProvider contractProvider, ILogger<TransactionJob> logger,
        IAlchemyOrderAppService alchemyOrderService,
        IOptionsSnapshot<TransactionOptions> options)
    {
        _contractProvider = contractProvider;
        _logger = logger;
        _alchemyOrderService = alchemyOrderService;
        _transactionOptions = options.Value;
    }

    public async Task HandleTransactionAsync(string chainId, string rawTransaction)
    {
        var transactionResult = await QueryTransactionAsync(chainId, rawTransaction);

        // retry transaction
        var times = 0;
        while (transactionResult.Status != TransactionState.Mined && _transactionOptions.RetryTime < times)
        {
            times++;
            await _contractProvider.SendRawTransaction(chainId, rawTransaction);
            transactionResult = await QueryTransactionAsync(chainId, rawTransaction);
        }

        if (transactionResult.Status != TransactionState.Mined)
        {
            //log
            return;
        }

        //send to ach
        await _alchemyOrderService.UpdateAlchemyTxHashAsync(new SendAlchemyTxHashDto()
        {
            //OrderId=
        });
    }

    private async Task<TransactionResultDto> QueryTransactionAsync(string chainId, string rawTransaction)
    {
        var transactionResult = await _contractProvider.GetTransactionResultAsync(chainId, rawTransaction);
        while (transactionResult.Status == TransactionState.Pending)
        {
            await Task.Delay(_transactionOptions.DelayTime);
            transactionResult = await _contractProvider.GetTransactionResultAsync(chainId, rawTransaction);
        }

        return transactionResult;
    }
    
}
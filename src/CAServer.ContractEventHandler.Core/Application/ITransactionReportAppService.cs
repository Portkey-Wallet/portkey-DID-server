using System;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Indexing.Elasticsearch;
using CAServer.DataReporting.Etos;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.HubsEventHandler;
using Hangfire;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.ContractEventHandler.Core.Application;

public interface ITransactionReportAppService
{
    Task HandleTransactionAsync(TransactionReportEto eventData);
}

public class TransactionReportAppService : ITransactionReportAppService, ISingletonDependency
{
    private readonly IContractProvider _contractProvider;
    private readonly ILogger<DataReportHandler> _logger;
    private readonly INESTRepository<CaHolderTransactionIndex, string> _transactionRepository;
    private readonly IObjectMapper _objectMapper;

    public TransactionReportAppService(IContractProvider contractProvider, ILogger<DataReportHandler> logger,
        INESTRepository<CaHolderTransactionIndex, string> transactionRepository, IObjectMapper objectMapper)
    {
        _contractProvider = contractProvider;
        _logger = logger;
        _transactionRepository = transactionRepository;
        _objectMapper = objectMapper;
    }

    public async Task HandleTransactionAsync(TransactionReportEto eventData)
    {
        // get result from contract
        // success -> return
        // pending or fail , save
        // pending -> hangfire
        try
        {
            var transactionResult =
                await _contractProvider.GetTransactionResultAsync(eventData.ChainId, eventData.TransactionId);
            _logger.LogInformation(
                "transaction status:{status}, chainId:{chainId}, caAddress:{caAddress}, transactionId:{transactionId}",
                transactionResult.Status, eventData.ChainId, eventData.CaAddress, eventData.TransactionId);

            if (transactionResult.Status == TransactionState.Mined)
            {
                return;
            }

            if (transactionResult.Status.IsNullOrEmpty() || transactionResult.Status == TransactionState.Pending)
            {
                // set in hangfire
                // BackgroundJob.Enqueue(() =>
                //     QueryTransactionAsync(
                //         _objectMapper.Map<TransactionReportEto, TransactionReportContext>(eventData)));
            }

            var status = transactionResult.Status == TransactionState.Pending
                ? TransactionState.Pending
                : TransactionState.Failed;

            //save
            // await _transactionRepository.AddOrUpdateAsync(new CaHolderTransactionIndex()
            // {
            //     ChainId = eventData.ChainId,
            //     CaAddress = eventData.CaAddress,
            //     BlockHash = transactionResult.BlockHash,
            //     BlockHeight = transactionResult.BlockNumber,
            //     MethodName = transactionResult.Transaction.MethodName,
            //     ToContractAddress = transactionResult.Transaction.To,
            //     Status = status,
            //     TransactionId = eventData.TransactionId
            // });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "handle report transaction error, data:{data}", JsonConvert.SerializeObject(eventData));
        }
    }

    private async Task QueryTransactionAsync(TransactionReportContext context)
    {
        var transactionResult = await GetTransactionResultAsync(context.ChainId, context.TransactionId);

        var transaction = await _transactionRepository.GetAsync(context.TransactionId);
        if (transaction == null)
        {
            _logger.LogWarning("handle transaction result in hangfire fail, transaction index is null, data:{context}",
                JsonConvert.SerializeObject(context));
            return;
        }

        if (transactionResult.Status == TransactionState.Mined)
        {
            await _transactionRepository.DeleteAsync(transaction);
        }

        transaction.Status = TransactionState.Failed;
        await _transactionRepository.AddOrUpdateAsync(transaction);
    }

    private async Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
    {
        await Task.Delay(5000);
        var transactionResult = await _contractProvider.GetTransactionResultAsync(chainId, transactionId);
        while (transactionResult.Status == TransactionState.Pending)
        {
            // await Task.Delay(_transactionOptions.CurrentValue.DelayTime);
            await Task.Delay(5000);
            transactionResult = await _contractProvider.GetTransactionResultAsync(chainId, transactionId);
        }

        return transactionResult;
    }
}
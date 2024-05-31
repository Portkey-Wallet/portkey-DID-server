using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.DataReporting.Etos;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.HubsEventHandler;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly IOptionsMonitor<TransactionReportOptions> _transactionReportOptions;

    public TransactionReportAppService(IContractProvider contractProvider, ILogger<DataReportHandler> logger,
        INESTRepository<CaHolderTransactionIndex, string> transactionRepository, IObjectMapper objectMapper,
        IOptionsMonitor<TransactionReportOptions> transactionReportOptions)
    {
        _contractProvider = contractProvider;
        _logger = logger;
        _transactionRepository = transactionRepository;
        _objectMapper = objectMapper;
        _transactionReportOptions = transactionReportOptions;
    }

    public async Task HandleTransactionAsync(TransactionReportEto eventData)
    {
        // success -> return, pending or fail -> save
        // pending -> hangfire
        try
        {
            var transactionResult =
                await _contractProvider.GetTransactionResultAsync(eventData.ChainId, eventData.TransactionId);
            _logger.LogInformation(
                "transaction status:{status}, chainId:{chainId}, caAddress:{caAddress}, transactionId:{transactionId}",
                transactionResult.Status, eventData.ChainId, eventData.CaAddress, eventData.TransactionId);

            if (transactionResult.Transaction == null)
            {
                _logger.LogWarning(
                    "transaction is null, status:{status}, chainId:{chainId}, caAddress:{caAddress}, transactionId:{transactionId}",
                    transactionResult.Status, eventData.ChainId, eventData.CaAddress, eventData.TransactionId);
                return;
            }
            
            if (transactionResult.Status == TransactionState.Mined)
            {
                return;
            }
            await SaveTransactionAsync(eventData, transactionResult);
            
            if (transactionResult.Status == TransactionState.Pending)
            {
                BackgroundJob.Enqueue(() =>
                    QueryTransactionAsync(
                        _objectMapper.Map<TransactionReportEto, TransactionReportContext>(eventData)));
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "handle report transaction error, data:{data}", JsonConvert.SerializeObject(eventData));
        }
    }

    private async Task SaveTransactionAsync(TransactionReportEto eventData,
        TransactionResultDto transactionResult)
    {
        var methodName = GetMethodName(transactionResult);
        var checkMethodName = ValidMethodName(transactionResult, methodName);
        if (!checkMethodName)
        {
            _logger.LogWarning("method no need to save, method:{method}", methodName);
            return;
        }

        var transactionIndex = GetTransactionIndex(eventData, transactionResult);
        await _transactionRepository.AddOrUpdateAsync(transactionIndex);
    }

    private CaHolderTransactionIndex GetTransactionIndex(TransactionReportEto eventData,
        TransactionResultDto transactionResult)
    {
        return new CaHolderTransactionIndex()
        {
            Id = eventData.TransactionId,
            ChainId = eventData.ChainId,
            CaAddress = eventData.CaAddress,
            BlockHash = transactionResult.BlockHash,
            BlockHeight = transactionResult.BlockNumber,
            MethodName = GetMethodName(transactionResult),
            ToContractAddress = transactionResult.Transaction.To,
            Status = GetStatus(transactionResult.Status),
            Timestamp = TimeHelper.GetTimeStampInSeconds(),
            TransactionId = eventData.TransactionId
        };
    }

    private string GetStatus(string status)
    {
        return status == TransactionState.Pending
            ? TransactionState.Pending
            : TransactionState.Failed;
    }

    private bool ValidMethodName(TransactionResultDto transactionDto, string methodName)
    {
        if (_transactionReportOptions.CurrentValue.ExcludeMethodNames.Contains(methodName))
        {
            return false;
        }

        return transactionDto.Transaction.MethodName == AElfContractMethodName.ManagerForwardCall ||
               (_transactionReportOptions.CurrentValue.MethodNames.IsNullOrEmpty() ||
                _transactionReportOptions.CurrentValue.MethodNames.Contains(transactionDto.Transaction.MethodName));
    }

    private string GetMethodName(TransactionResultDto transactionDto)
    {
        if (transactionDto.Transaction.MethodName != AElfContractMethodName.ManagerForwardCall)
        {
            return transactionDto.Transaction.MethodName;
        }

        var managerForwardCallInfo =
            JsonConvert.DeserializeObject<ManagerForwardCallInfoDto>(transactionDto.Transaction.Params);
        return managerForwardCallInfo.MethodName;
    }

    public async Task QueryTransactionAsync(TransactionReportContext context)
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
            return;
        }

        transaction.Status = TransactionState.Failed;
        await _transactionRepository.AddOrUpdateAsync(transaction);
    }

    private async Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
    {
        await Task.Delay(_transactionReportOptions.CurrentValue.QueryInterval);
        var transactionResult = await _contractProvider.GetTransactionResultAsync(chainId, transactionId);
        while (transactionResult.Status == TransactionState.Pending)
        {
            await Task.Delay(_transactionReportOptions.CurrentValue.QueryInterval);
            transactionResult = await _contractProvider.GetTransactionResultAsync(chainId, transactionId);
        }

        return transactionResult;
    }
}
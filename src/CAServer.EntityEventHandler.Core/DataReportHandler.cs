using System;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Types;
using CAServer.Common;
using CAServer.DataReporting.Etos;
using CAServer.Grains.Grain.ApplicationHandler;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class DataReportHandler : IDistributedEventHandler<TransactionReportEto>, ITransientDependency
{
    private readonly IContractProvider _contractProvider;
    private readonly ILogger<DataReportHandler> _logger;

    public DataReportHandler(IContractProvider contractProvider, ILogger<DataReportHandler> logger)
    {
        _contractProvider = contractProvider;
        _logger = logger;
    }

    public async Task HandleEventAsync(TransactionReportEto eventData)
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

            if (transactionResult.Status == TransactionState.Pending)
            {
                // hangfire
            }

            //save
        }
        catch (Exception e)
        {
            _logger.LogError(e, "handle report transaction error, data:{data}", JsonConvert.SerializeObject(eventData));
        }
    }

    // private async Task<TransactionResultDto> QueryTransactionAsync(string chainId)
    // {
    //     var transactionId = transaction.GetHash().ToHex();
    //     return await _contractProvider.GetTransactionResultAsync(chainId, transactionId);
    //     // while (transactionResult.Status == TransactionState.Pending)
    //     // {
    //     //     // await Task.Delay(_transactionOptions.CurrentValue.DelayTime);
    //     //     transactionResult = await _contractProvider.GetTransactionResultAsync(chainId, transactionId);
    //     // }
    // }
}
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Nightingale;
using CAServer.ScheduledTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CAServer.ContractEventHandler.Core.Worker;

public class ContractSyncWorker : ScheduledTaskBase
{
    private readonly IContractAppService _contractAppService;
    private readonly N9EClientFactory _n9EClientFactory;
    private readonly ILogger<ContractSyncWorker> _logger;
    private const string WorkerName = "ContractSyncWorker";
    
    public ContractSyncWorker(
        IContractAppService contractAppService, IOptions<ContractSyncOptions> workerOptions,
        N9EClientFactory n9EClientFactory, ILogger<ContractSyncWorker> logger)
    {
        _contractAppService = contractAppService;
        _n9EClientFactory = n9EClientFactory;
        Period = workerOptions.Value.Sync;
        _logger = logger;
    }

    protected override async Task DoWorkAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("ContractSyncWorker DoWorkAsync start");
            await _contractAppService.QueryAndSyncAsync();
            _logger.LogInformation("ContractSyncWorker DoWorkAsync end");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ContractSyncWorker DoWorkAsync has error");
        }
        finally
        {
            stopwatch.Stop();
            await _n9EClientFactory.TrackTransactionSync(N9EClientConstant.Biz, WorkerName,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
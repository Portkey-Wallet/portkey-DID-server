using System.Diagnostics;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Nightingale;
using CAServer.ScheduledTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CAServer.ContractEventHandler.Core.Worker;

public class TransferAutoReceiveWorker : ScheduledTaskBase
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly N9EClientFactory _n9EClientFactory;
    private readonly ILogger<TransferAutoReceiveWorker> _logger;

    private const string WorkerName = "TransferAutoReceiveWorker";

    public TransferAutoReceiveWorker(
        ICrossChainTransferAppService crossChainTransferAppService,
        IOptions<ContractSyncOptions> workerOptions,
        N9EClientFactory n9EClientFactory,
        ILogger<TransferAutoReceiveWorker> logger)
    {
        _crossChainTransferAppService = crossChainTransferAppService;
        _n9EClientFactory = n9EClientFactory;
        _logger = logger;

        Period = workerOptions.Value.AutoReceive;
    }

    protected override async Task DoWorkAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[AutoReceive] TransferAutoReceiveWorker start.");
            await _crossChainTransferAppService.AutoReceiveAsync();
            _logger.LogInformation("[AutoReceive] TransferAutoReceiveWorker end.");
        }
        finally
        {
            stopwatch.Stop();
            await _n9EClientFactory.TrackTransactionSync(N9EClientConstant.Biz, WorkerName,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
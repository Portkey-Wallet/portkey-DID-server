using System.Diagnostics;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Nightingale;
using CAServer.ScheduledTask;
using Microsoft.Extensions.Options;

namespace CAServer.ContractEventHandler.Core.Worker;

public class TransferAutoReceiveWorker : ScheduledTaskBase
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly N9EClientFactory _n9EClientFactory;

    private const string WorkerName = "TransferAutoReceiveWorker";

    public TransferAutoReceiveWorker(
        ICrossChainTransferAppService crossChainTransferAppService,
        IOptions<ContractSyncOptions> workerOptions,
        N9EClientFactory n9EClientFactory)
    {
        _crossChainTransferAppService = crossChainTransferAppService;
        _n9EClientFactory = n9EClientFactory;

        Period = workerOptions.Value.AutoReceive;
    }

    protected override async Task DoWorkAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _crossChainTransferAppService.AutoReceiveAsync();
        }
        finally
        {
            stopwatch.Stop();
            await _n9EClientFactory.TrackTransactionSync(N9EClientConstant.Biz, WorkerName,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.ScheduledTask;
using Microsoft.Extensions.Options;

namespace CAServer.ContractEventHandler.Core.Worker;

public class ChainHeightWorker : ScheduledTaskBase
{
    private readonly IChainHeightService _chainHeightService;

    public ChainHeightWorker(IChainHeightService chainHeightService, IOptionsSnapshot<SyncChainHeightOptions> options)
    {
        Period = options.Value.Period;
        _chainHeightService = chainHeightService;
    }

    protected override async Task DoWorkAsync()
    {
        await _chainHeightService.SetChainHeightAsync();
    }
}
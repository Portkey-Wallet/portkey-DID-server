using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.FreeMint.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ContractEventHandler.Core;

public class FreeMintEventHandler : IDistributedEventHandler<FreeMintEto>, ITransientDependency
{
    private readonly IMintNftItemService _mintNftItemService;
    private readonly ILogger<FreeMintEventHandler> _logger;

    public FreeMintEventHandler(IMintNftItemService mintNftItemService, ILogger<FreeMintEventHandler> logger)
    {
        _mintNftItemService = mintNftItemService;
        _logger = logger;
    }

    public async Task HandleEventAsync(FreeMintEto eventData)
    {
        _logger.LogInformation("Begin Handle FreeMint: data:{data}", JsonConvert.SerializeObject(eventData));
        _ = _mintNftItemService.MintAsync(eventData);
    }
}
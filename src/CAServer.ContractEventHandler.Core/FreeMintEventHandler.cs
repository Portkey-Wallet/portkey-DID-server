using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.FreeMint.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ContractEventHandler.Core;

public class FreeMintEventHandler : IDistributedEventHandler<FreeMintEto>, ITransientDependency
{
    private readonly IMintNftItemService _mintNftItemService;

    public FreeMintEventHandler(IMintNftItemService mintNftItemService)
    {
        _mintNftItemService = mintNftItemService;
    }

    public async Task HandleEventAsync(FreeMintEto eventData)
    {
        _ = _mintNftItemService.MintAsync(eventData);
    }
}
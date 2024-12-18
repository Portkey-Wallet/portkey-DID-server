using System.Threading.Tasks;
using CAServer.EntityEventHandler.Core.Service;
using CAServer.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
namespace CAServer.EntityEventHandler.Core;

public class CAHolderHandler : IDistributedEventHandler<CreateUserEto>,
    IDistributedEventHandler<UpdateCAHolderEto>,
    IDistributedEventHandler<DeleteCAHolderEto>
    , ITransientDependency
{
    private readonly ICaHolderService _caHolderService;

    public CAHolderHandler(ICaHolderService caHolderService)
    {
        _caHolderService = caHolderService;
    }

    public async Task HandleEventAsync(CreateUserEto eventData)
    {
        _ = _caHolderService.CreateUserAsync(eventData);
    }

    public async Task HandleEventAsync(UpdateCAHolderEto eventData)
    {
        _ = _caHolderService.UpdateCaHolderAsync(eventData);
    }

    public async Task HandleEventAsync(DeleteCAHolderEto eventData)
    {
        _ = _caHolderService.DeleteCaHolderAsync(eventData);
    }
}
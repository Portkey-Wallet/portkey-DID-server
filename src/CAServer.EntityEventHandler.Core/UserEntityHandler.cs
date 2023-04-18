using System.Threading.Tasks;
using CAServer.Tokens;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.EntityEventHandler.Core;

public class UserEntityHandler : EntityHandlerBase,
    IDistributedEventHandler<EntityCreatedEto<UserEto>>
{
    private readonly IUserTokenAppService _userTokenAppService;
    public UserEntityHandler(IUserTokenAppService userTokenAppService)
    {
        _userTokenAppService = userTokenAppService;
    }
    public async Task HandleEventAsync(EntityCreatedEto<UserEto> eventData)
    {
        var userId = eventData.Entity.Id;
        await _userTokenAppService.AddUserTokenAsync(userId);
    }
}
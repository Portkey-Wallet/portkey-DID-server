using System.Threading.Tasks;
using CAServer.EntityEventHandler.Core.Service;
using CAServer.Tokens.Etos;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class UserTokenEntityHandler : EntityHandlerBase,
    IDistributedEventHandler<UserTokenEto>,
    IDistributedEventHandler<UserTokenDeleteEto>
{
    private readonly IUserTokenService _userTokenService;

    public UserTokenEntityHandler(IUserTokenService userTokenService)
    {
        _userTokenService = userTokenService;
    }

    public async Task HandleEventAsync(UserTokenEto eventData)
    {
        _ = _userTokenService.AddTokenAsync(eventData);
    }

    public async Task HandleEventAsync(UserTokenDeleteEto eventData)
    {
        _ = _userTokenService.DeleteTokenAsync(eventData);
    }
}
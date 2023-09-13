using System;
using System.Threading.Tasks;
using CAServer.Etos;
using CAServer.UserAssets;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class UserLoginHandler : IDistributedEventHandler<UserLoginEto>
{
    private readonly IUserAssetsAppService _userAssetsAppService;
    
    public UserLoginHandler(IUserAssetsAppService userAssetsAppService)
    {
        _userAssetsAppService = userAssetsAppService;
    }
    
    public async Task HandleEventAsync(UserLoginEto eventData)
    {
        await _userAssetsAppService.CheckOriginChainIdStatusAsync(eventData);
    }
}
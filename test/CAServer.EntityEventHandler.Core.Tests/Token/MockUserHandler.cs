using CAServer.Etos;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Tests.Token;

public class MockUserHandler:IDistributedEventHandler<CreateUserEto>,ITransientDependency
{
    private readonly IUserTokenAppService _userTokenAppService;

    public MockUserHandler(IUserTokenAppService userTokenAppService)
    {
        _userTokenAppService = userTokenAppService;
        
    }

    public async Task HandleEventAsync(CreateUserEto eventData)
    {
        await _userTokenAppService.AddUserTokenAsync(eventData.UserId, new AddUserTokenInput());
    }
}
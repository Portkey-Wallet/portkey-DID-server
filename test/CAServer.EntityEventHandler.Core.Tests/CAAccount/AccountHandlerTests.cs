using CAServer.Account;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Hubs;
using CAServer.Search;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.CAAccount;

public class   AccountHandlerTests : CAServerEntityEventHandlerTestBase
{
    private readonly ISearchAppService _searchAppService;
    private readonly IDistributedEventBus _eventBus;

    public AccountHandlerTests()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
        _eventBus = GetRequiredService<IDistributedEventBus>();
    }

    [Fact]
    public async Task HandlerEvent_AccountRegisterCreate()
    {
        var chain = new AccountRegisterCreateEto
        {
            Id = Guid.NewGuid(),
            GuardianInfo = new GuardianInfo(),
            RegisteredTime = new DateTime(),
            RegisterSuccess = true,
            RegisterMessage = "test",
            RegisterStatus = "",
            Context = new HubRequestContext()
           
        };
        await _eventBus.PublishAsync(chain);

        var result = await _searchAppService.GetListByLucenceAsync("accountregisterindex", new GetListInput()
        {
            MaxResultCount = 1
        });

        result.ShouldNotBeNull();
        var extra = JsonConvert.DeserializeObject<PagedResultDto<AccountRegisterIndex>>(result);
        extra.ShouldNotBeNull();
        extra.Items[0].RegisterMessage.ShouldBe("test");
    }
    
    [Fact]
    public async Task HandlerEvent_AccountRecoverCreate()
    {
        var chain = new AccountRecoverCreateEto
        {
            Id = Guid.NewGuid(),
            GrainId = "",
            RecoverySuccess = true,
            RecoveryMessage = "test",
            RecoveryTime = new DateTime(),
            LoginGuardianIdentifierHash = "",
            Context = new HubRequestContext()
        };
        await _eventBus.PublishAsync(chain);

        var result = await _searchAppService.GetListByLucenceAsync("accountrecoverindex", new GetListInput()
        {
            MaxResultCount = 1
        });

        result.ShouldNotBeNull();
        var extra = JsonConvert.DeserializeObject<PagedResultDto<AccountRecoverIndex>>(result);
        extra.ShouldNotBeNull();
        extra.Items[0].RecoveryMessage.ShouldBe("test");
    }
    
}
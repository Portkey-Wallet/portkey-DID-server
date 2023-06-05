using CAServer.Account;
using CAServer.ContractEventHandler;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Hubs;
using CAServer.Orleans.TestBase;
using CAServer.Search;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.CAAccount;

public partial class AccountHandlerTests : CAServerEntityEventHandlerTestBase
{
    private readonly ISearchAppService _searchAppService;

    private readonly IDistributedEventBus _eventBus;
    //private readonly TestCluster _cluster;

    public AccountHandlerTests()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
        _eventBus = GetRequiredService<IDistributedEventBus>();
        // _cluster = GetRequiredService<ClusterFixture>().Cluster;
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetClusterClient());
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

    [Fact]
    public async Task HandlerEvent_CreateHolder_Test()
    {
        var id = Guid.NewGuid();
        var register = new AccountRegisterCreateEto
        {
            Id = id,
            GrainId = "registerTest",
            GuardianInfo = new GuardianInfo(),
            RegisteredTime = new DateTime(),
            RegisterSuccess = false,
            RegisterMessage = "test",
            RegisterStatus = "",
            Context = new HubRequestContext()
        };
        await _eventBus.PublishAsync(register);

        var eto = new CreateHolderEto
        {
            Id = id,
            GrainId = "registerTest",
            RegisterSuccess = true,
            RegisterMessage = "test",
            Context = new HubRequestContext()
        };
        await _eventBus.PublishAsync(eto);

        var result = await _searchAppService.GetListByLucenceAsync("accountregisterindex", new GetListInput()
        {
            MaxResultCount = 1
        });

        result.ShouldNotBeNull();
        var extra = JsonConvert.DeserializeObject<PagedResultDto<AccountRegisterIndex>>(result);
        extra.ShouldNotBeNull();
        extra.Items[0].RegisterSuccess.ShouldBe(true);
    }

    [Fact]
    public async Task HandlerEvent_AccountRecoverUpdate_Test()
    {
        var id = Guid.NewGuid();
        var recovery = new AccountRecoverCreateEto
        {
            Id = id,
            GrainId = "recovery_test",
            RecoverySuccess = false,
            RecoveryMessage = "test",
            RecoveryTime = new DateTime(),
            LoginGuardianIdentifierHash = "test",
            Context = new HubRequestContext()
        };
        await _eventBus.PublishAsync(recovery);

        var eto = new SocialRecoveryEto
        {
            Id = id,
            GrainId = "recovery_test",
            RecoverySuccess = true,
            RecoveryMessage = "test",
            RecoveryTime = new DateTime(),
            CaAddress = "address",
            CaHash = "hash",
            Context = new HubRequestContext()
        };
        await _eventBus.PublishAsync(eto);

        var result = await _searchAppService.GetListByLucenceAsync("accountrecoverindex", new GetListInput()
        {
            MaxResultCount = 1
        });

        result.ShouldNotBeNull();
        var extra = JsonConvert.DeserializeObject<PagedResultDto<AccountRecoverIndex>>(result);
        extra.ShouldNotBeNull();
        extra.Items[0].RecoverySuccess.ShouldBe(true);
    }
}
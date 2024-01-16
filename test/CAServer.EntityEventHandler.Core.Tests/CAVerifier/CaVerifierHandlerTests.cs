using CAServer.Entities.Es;
using CAServer.Search;
using CAServer.Verifier.Etos;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.CAVerifier;

public partial class CaVerifierHandlerTests : CAServerEntityEventHandlerTestBase
{
    private readonly ISearchAppService _searchAppService;
    private readonly IDistributedEventBus _eventBus;

    public CaVerifierHandlerTests()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
        _eventBus = GetRequiredService<IDistributedEventBus>();
    }
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetClient());
        services.AddSingleton(GetMockAbpDistributedLock());
    }
    
    [Fact]
    public async Task HandlerEvent_NewNotity()
    {
        var verifer = new VerifierCodeEto
        {
            VerifierSessionId = Guid.NewGuid(),
            Type = "1",
            GuardianAccount = "test",
            VerifierId = "test0000322342",
            ChainId = "test0032351",
        };
        await _eventBus.PublishAsync(verifer);
    }
}
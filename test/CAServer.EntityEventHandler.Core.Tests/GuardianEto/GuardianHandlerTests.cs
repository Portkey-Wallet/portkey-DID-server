using CAServer.Entities.Es;
using CAServer.EntityEventHandler.Tests.Notify;
using CAServer.Guardian;
using CAServer.Search;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.Notity;

public class  GuardianHandlerTests : CAServerEntityEventHandlerTestBase
{
    private readonly ISearchAppService _searchAppService;
    private readonly IDistributedEventBus _eventBus;

    public GuardianHandlerTests()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
        _eventBus = GetRequiredService<IDistributedEventBus>();
    }

    [Fact]
    public async Task HandlerEvent_NewNotity()
    {
        var guardian = new GuardianEto
        {
            Id = Guid.NewGuid().ToString(),
            Identifier = "test",
            IdentifierHash = "test",
            Salt = "test",
        };
        await _eventBus.PublishAsync(guardian);

        var result = await _searchAppService.GetListByLucenceAsync("guardianindex", new GetListInput()
        {
            MaxResultCount = 1
        });

        result.ShouldNotBeNull();
        var info = JsonConvert.DeserializeObject<PagedResultDto<GuardianIndex>>(result);
        info.ShouldNotBeNull();
        info.Items[0].Identifier.ShouldBe("test");

    }
}
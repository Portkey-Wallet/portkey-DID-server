using CAServer.Bookmark.Etos;
using CAServer.Search;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.Bookmark;

public class BookmarkHandlerTest : CAServerEntityEventHandlerTestBase
{
    private readonly ISearchAppService _searchAppService;
    private readonly IDistributedEventBus _eventBus;

    public BookmarkHandlerTest()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
        _eventBus = GetRequiredService<IDistributedEventBus>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetMockAbpDistributedLock());
    }

    [Fact]
    public async Task CreateTest()
    {
        var bookmark = new BookmarkCreateEto
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "test",
            Url = "test",
            ModificationTime = DateTime.UtcNow.Microsecond,
            Index = 1
        };
        await _eventBus.PublishAsync(bookmark);
    }
}
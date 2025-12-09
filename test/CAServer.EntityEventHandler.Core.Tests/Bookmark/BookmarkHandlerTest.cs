using AElf.Indexing.Elasticsearch;
using CAServer.Bookmark.Etos;
using CAServer.Entities.Es;
using CAServer.Search;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.Bookmark;

public class BookmarkHandlerTest : CAServerEntityEventHandlerTestBase
{
    private readonly ISearchAppService _searchAppService;
    private readonly IDistributedEventBus _eventBus;
    private readonly INESTRepository<BookmarkIndex, Guid> _bookMarkRepository;

    public BookmarkHandlerTest()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
        _eventBus = GetRequiredService<IDistributedEventBus>();
        _bookMarkRepository = GetRequiredService<INESTRepository<BookmarkIndex, Guid>>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
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

    [Fact]
    public async Task BookmarkDeleteTest()
    {
        var userId = Guid.NewGuid();
        await _bookMarkRepository.AddAsync(new BookmarkIndex()
        {
            Id = Guid.Empty,
            UserId = userId
        });

        var bookmark = new BookmarkDeleteEto
        {
            UserId = Guid.NewGuid()
        };

        await _eventBus.PublishAsync(bookmark);
    }

    [Fact]
    public async Task BookmarkMultiDeleteTest()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await _bookMarkRepository.AddAsync(new BookmarkIndex()
        {
            Id = id,
            UserId = userId
        });

        var bookmark = new BookmarkMultiDeleteEto
        {
            UserId = userId,
            Ids = new List<Guid>() { id }
        };
        await _eventBus.PublishAsync(bookmark);
    }
}
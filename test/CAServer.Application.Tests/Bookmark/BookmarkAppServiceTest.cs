using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Bookmark.Dtos;
using CAServer.Bookmark.Etos;
using CAServer.Entities.Es;
using CAServer.EntityEventHandler.Core;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace CAServer.Bookmark;

public sealed class BookmarkAppServiceTest : CAServerApplicationTestBase
{
    private readonly BookmarkAppService _bookmarkAppService;

    public BookmarkAppServiceTest()
    {
        _bookmarkAppService = GetRequiredService<BookmarkAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetMockAbpDistributedLock());
    }

    [Fact]
    public async void CreateTest()
    {
        await _bookmarkAppService.CreateAsync(new CreateBookmarkDto()
        {
            Name = "name1",
            Url = "http://url.com"
        });

        var pageResult = await _bookmarkAppService.GetBookmarksAsync(new GetBookmarksDto()
        {
            SkipCount = 0,
            MaxResultCount = 10
        });
        pageResult.TotalCount.ShouldBe(1);

        var bookMark = _bookmarkAppService.GetBookmarkGrain(1);
        var count = await bookMark.GetItemCount();
        count.ShouldBe(1);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Bookmark.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp;
using Xunit;
using Xunit.Sdk;

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
        base.AfterAddApplication(services);
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

    [Fact]
    public async void GrainTailTest()
    {
        #region init state tail page is 1
        
        var bookmarkMeta = _bookmarkAppService.GetBookmarkMetaGrain();
        var tail = await bookmarkMeta.GetTailBookMarkGrainIndex();
        tail.ShouldBe(1);
        
        #endregion

        #region fill one page data
        
        for (var i = 0 ; i < 100 ; i++ )
        {
            await _bookmarkAppService.CreateAsync(new CreateBookmarkDto()
            {
                Name = "name_" + i,
                Url = "url_" + i
            });
        }
        
        #endregion

        #region tail page is 2
        
        tail = await bookmarkMeta.GetTailBookMarkGrainIndex();
        tail.ShouldBe(2);

        var bookmark = _bookmarkAppService.GetBookmarkGrain(1);
        var count = await bookmark.GetItemCount();
        count.ShouldBe(100);
        
        bookmark = _bookmarkAppService.GetBookmarkGrain(2);
        count = await bookmark.GetItemCount();
        count.ShouldBe(0);

        #endregion

        #region add item to second page
        
        await _bookmarkAppService.CreateAsync(new CreateBookmarkDto()
        {
            Name = "name_" + 101,
            Url = "url_" + 101
        });

        bookmark = _bookmarkAppService.GetBookmarkGrain(2);
        count = await bookmark.GetItemCount();
        count.ShouldBe(1);

        #endregion

        #region query page
        var getPage = await _bookmarkAppService.GetBookmarksAsync(new GetBookmarksDto()
        {
            SkipCount = 10,
            MaxResultCount = 10,
        });
        getPage.TotalCount.ShouldBe(101);
        getPage.Items.Count().ShouldBe(10);
        getPage.Items[0].Name.ShouldBe("name_90");
        #endregion
        
        #region delete last 2 items
        
        var list = await _bookmarkAppService.GetBookmarksAsync(new GetBookmarksDto()
        {
            SkipCount = 0,
            MaxResultCount = 2
        });
        list.TotalCount.ShouldBe(101);
        list.Items.Count.ShouldBe(2);
        
        var first = list.Items[0] ?? new BookmarkResultDto();
        first.Id.ShouldNotBe(new Guid());
        first.Index.ShouldBe(2);
        
        var second = list.Items[1] ?? new BookmarkResultDto();
        second.Id.ShouldNotBe(new Guid());
        second.Index.ShouldBe(1);

        await _bookmarkAppService.DeleteListAsync(new()
        {
            DeleteInfos = new List<BookmarkInfo>()
            {
                new()
                {
                    Id = first.Id,
                    Index = first.Index
                },
                new()
                {
                    Id = second.Id,
                    Index = second.Index
                }
            }
        });
        
        #endregion

        #region tail page is 1

        bookmarkMeta = _bookmarkAppService.GetBookmarkMetaGrain();
        tail = await bookmarkMeta.GetTailBookMarkGrainIndex();
        tail.ShouldBe(1);

        #endregion

        #region delate all

        await _bookmarkAppService.DeleteAsync();
        
        bookmarkMeta = _bookmarkAppService.GetBookmarkMetaGrain();
        tail = await bookmarkMeta.GetTailBookMarkGrainIndex();
        tail.ShouldBe(1);
        
        bookmark = _bookmarkAppService.GetBookmarkGrain(1);
        count = await bookmark.GetItemCount();
        count.ShouldBe(0);
        
        bookmark = _bookmarkAppService.GetBookmarkGrain(2);
        count = await bookmark.GetItemCount();
        count.ShouldBe(0);
        
        #endregion

    }

    [Fact]
    public async void QueryPage()
    {
        for (var i = 0; i < 12; i++)
        {
            await _bookmarkAppService.CreateAsync(new CreateBookmarkDto()
            {
                Name = "name_" + i,
                Url = "url_" + i
            });
        }

        var pageResult = await _bookmarkAppService.GetBookmarksAsync(new()
        {
            SkipCount = 3,
            MaxResultCount = 8
        });

        pageResult.TotalCount.ShouldBe(12);
        pageResult.Items.Count.ShouldBe(8);

        pageResult = await _bookmarkAppService.GetBookmarksAsync(new()
        {
            SkipCount = 13,
            MaxResultCount = 10
        });
        pageResult.TotalCount.ShouldBe(12);
        pageResult.Items.Count.ShouldBe(0);
        
    }

    [Fact]
    public async void DistributedLockTestCreate()
    {
        var tasks = new List<Task>();
        for (var i = 0; i < 12; i++)
            tasks.Add(_bookmarkAppService.CreateAsync(new CreateBookmarkDto()
            {
                Name = "name_" + i,
                Url = "url_" + i
            }));

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => Task.WhenAll(tasks));
        exception.ShouldNotBeNull();
        exception.Message.ShouldContain("Get lock fail");
    }
    
    [Fact]
    public async void DistributedLockTestDelete()
    {
        var tasks = new List<Task>();
        for (var i = 0; i < 12; i++)
            tasks.Add(_bookmarkAppService.DeleteAsync());

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => Task.WhenAll(tasks));
        exception.ShouldNotBeNull();
        exception.Message.ShouldContain("Get lock fail");
    }
    
    [Fact]
    public async void DistributedLockTestDeleteList()
    {
        var tasks = new List<Task>();
        for (var i = 0; i < 12; i++)
            tasks.Add(_bookmarkAppService.DeleteListAsync(new DeleteBookmarkDto()
            {
                DeleteInfos = new List<BookmarkInfo>()
                {
                    new BookmarkInfo()
                    {
                        Id = Guid.Empty,
                        Index = i
                    }
                }
            }));

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => Task.WhenAll(tasks));
        exception.ShouldNotBeNull();
        exception.Message.ShouldContain("Get lock fail");
    }
    

}
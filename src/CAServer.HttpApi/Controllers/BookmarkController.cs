using System.Threading.Tasks;
using CAServer.Bookmark;
using CAServer.Bookmark.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Bookmark")]
[Route("api/app/bookmarks/")]
[Authorize]
public class BookmarkController : CAServerController
{
    private readonly IBookmarkAppService _bookmarkAppService;

    public BookmarkController(IBookmarkAppService bookmarkAppService)
    {
        _bookmarkAppService = bookmarkAppService;
    }

    [HttpPost]
    public async Task CreateAsync(CreateBookmarkDto input)
    {
        await _bookmarkAppService.CreateAsync(input);
    }

    [HttpGet]
    public async Task<PagedResultDto<BookmarkResultDto>> GetBookmarksAsync(GetBookmarksDto input)
    {
        return await _bookmarkAppService.GetBookmarksAsync(input);
    }

    [HttpDelete]
    public async Task DeleteAsync()
    {
        await _bookmarkAppService.DeleteAsync();
    }

    [HttpDelete("deleteBookmarks")]
    public async Task DeleteBookmarksAsync(DeleteBookmarkDto input)
    {
        await _bookmarkAppService.DeleteAsync();
    }

    [HttpPost("sort")]
    public async Task SortAsync(SortBookmarksDto input)
    {
        await _bookmarkAppService.SortAsync(input);
    }
}
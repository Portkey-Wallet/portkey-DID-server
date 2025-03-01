using System.Threading.Tasks;
using Asp.Versioning;
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
[IgnoreAntiforgeryToken]
public class BookmarkController : CAServerController
{
    private readonly IBookmarkAppService _bookmarkAppService;

    public BookmarkController(IBookmarkAppService bookmarkAppService)
    {
        _bookmarkAppService = bookmarkAppService;
    }

    [HttpPost]
    public async Task<BookmarkResultDto> CreateAsync(CreateBookmarkDto input)
    {
        return await _bookmarkAppService.CreateAsync(input);
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

    [HttpPost("modify")]
    public async Task ModifyAsync(DeleteBookmarkDto input)
    {
        await _bookmarkAppService.DeleteListAsync(input);
    }
}
using System.Threading.Tasks;
using CAServer.Bookmark.Dtos;
using Volo.Abp.Application.Dtos;

namespace CAServer.Bookmark;

public interface IBookmarkAppService
{
    Task<BookmarkResultDto> CreateAsync(CreateBookmarkDto input);
    Task<PagedResultDto<BookmarkResultDto>> GetBookmarksAsync(GetBookmarksDto input);
    Task DeleteAsync();
    Task DeleteListAsync(DeleteBookmarkDto input);
}
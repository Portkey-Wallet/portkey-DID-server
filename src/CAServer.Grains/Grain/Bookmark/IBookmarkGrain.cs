using CAServer.Bookmark.Dtos;
using CAServer.Grains.Grain.Bookmark.Dtos;

namespace CAServer.Grains.Grain.Bookmark;

public interface IBookmarkGrain : IGrainWithStringKey
{
    Task<GrainResultDto> DeleteAll();
    Task<GrainResultDto<BookmarkGrainResultDto>> AddBookMark(BookmarkGrainDto grainDto);
    Task<GrainResultDto<List<BookmarkGrainResultDto>>> DeleteItems(List<Guid> ids);
    Task<List<BookmarkResultDto>> GetRange(int from, int to);
    Task<int> GetItemCount();
}
using CAServer.Grains.Grain.Bookmark.Dtos;
using Orleans;

namespace CAServer.Grains.Grain.Bookmark;

public interface IBookmarkGrain : IGrainWithStringKey
{
    Task<GrainResultDto> DeleteAll();
    Task<GrainResultDto<BookmarkGrainResultDto>> AddBookMark(BookmarkGrainDto grainDto);
    Task<GrainResultDto<List<BookmarkGrainResultDto>>> DeleteItems(List<Guid> ids);
    Task<int> GetItemCount();
}
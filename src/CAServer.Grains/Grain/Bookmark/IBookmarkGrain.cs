using CAServer.Grains.Grain.Bookmark.Dtos;

namespace CAServer.Grains.Grain.Bookmark;

public interface IBookmarkGrain
{
    Task<GrainResultDto> DeleteAll();
    Task<GrainResultDto<BookmarkGrainResultDto>> AddBookMark(BookmarkGrainDto grainDto);
    Task<GrainResultDto<List<BookmarkGrainResultDto>>> DeleteItems(List<Guid> ids);

}
using CAServer.Commons;
using CAServer.Grains.State.Bookmark;
using CAServer.Grains.Grain.Bookmark.Dtos;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Bookmark;

public class BookmarkGrain : Grain<BookmarkState>, IBookmarkGrain
{
    private readonly IObjectMapper _objectMapper;

    public BookmarkGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<GrainResultDto<BookmarkGrainResultDto>> AddBookMark(BookmarkGrainDto grainDto)
    {
        var result = new GrainResultDto<BookmarkGrainResultDto>();
        if (string.IsNullOrWhiteSpace(State.Id))
        {
            State.Id = this.GetPrimaryKeyString();
            State.UserId = grainDto.UserId;
        }

        var exist = State.BookmarkItems.Any(t => t.Name == grainDto.Name && t.Url == grainDto.Url);
        if (exist)
        {
            result.Message = "The bookmark already exist.";
            return result;
        }

        var item = new BookmarkItem()
        {
            Id = Guid.NewGuid(),
            Name = grainDto.Name,
            Url = grainDto.Url,
            ModificationTime = TimeHelper.GetTimeStampInMilliseconds(),
            GrainIndex = grainDto.GrainIndex
        };

        State.BookmarkItems.Add(item);

        await WriteStateAsync();
        result.Success = true;
        result.Data = _objectMapper.Map<BookmarkItem, BookmarkGrainResultDto>(item);
        return result;
    }

    public async Task<GrainResultDto> DeleteAll()
    {
        var result = new GrainResultDto();
        if (string.IsNullOrWhiteSpace(State.Id))
        {
            result.Message = "Not exist.";
            return result;
        }

        State.BookmarkItems = new List<BookmarkItem>();
        await WriteStateAsync();

        result.Success = true;
        return result;
    }

    public async Task<GrainResultDto<List<BookmarkGrainResultDto>>> DeleteItems(List<Guid> ids)
    {
        var result = new GrainResultDto<List<BookmarkGrainResultDto>>();
        if (string.IsNullOrWhiteSpace(State.Id))
        {
            result.Message = "Not exist.";
            return result;
        }

        var items = State.BookmarkItems.Where(t => ids.Contains(t.Id)).ToList();
        State.BookmarkItems.RemoveAll(items);
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<List<BookmarkItem>, List<BookmarkGrainResultDto>>(items);
        return result;
    }
}
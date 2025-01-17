using CAServer.Bookmark.Dtos;
using CAServer.Commons;
using CAServer.Grains.Grain.Bookmark.Dtos;
using CAServer.Grains.State.Bookmark;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Bookmark;

public class BookmarkGrain : Grain<BookmarkState>, IBookmarkGrain
{
    private readonly IObjectMapper _objectMapper;

    public BookmarkGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, token);
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
            Index = grainDto.Index
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

    public async Task<List<BookmarkResultDto>> GetRange(int from, int to)
    {
        from = Math.Max(from, 0);
        to = Math.Min(to, State.BookmarkItems.Count);
        if (from > to)
            return new List<BookmarkResultDto>();
        var items = from > to ? new List<BookmarkItem>() : State.BookmarkItems.GetRange(from, to - from + 1);
        return _objectMapper.Map<List<BookmarkItem>, List<BookmarkResultDto>>(items);
    }

    public Task<int> GetItemCount()
    {
        return Task.FromResult(State.BookmarkItems.Count);
    }
}
using CAServer.Grains.State.Bookmark;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Bookmark;

public class BookmarkMetaGrain: Grain<BookmarkMetaState>, IBookmarkMetaGrain
{
    private readonly IObjectMapper _objectMapper;

    public BookmarkMetaGrain(IObjectMapper objectMapper)
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
}
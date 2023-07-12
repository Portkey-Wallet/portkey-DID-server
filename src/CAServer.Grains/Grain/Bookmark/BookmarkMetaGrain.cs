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

    public int GetTailBookMarkGrainIndex()
    {
        if (State.Items.IsNullOrEmpty())
        {
            State.Items.Add(new BookMarkMetaItem() { GrainIndex = 1 });
            return 1;
        }
        var hasDataList = State.Items.Where(item => item.Size > 0).ToList();
        if (hasDataList.IsNullOrEmpty())
            return 1;
        var tail = hasDataList[hasDataList.Count - 1];
        if (tail.Size >= 100)
        {
            var idx = tail.GrainIndex;
            State.Items.Add(new BookMarkMetaItem() { GrainIndex = idx });
            return idx;
        }
        return tail.GrainIndex;
    }

    public List<BookMarkMetaItem> RemoveAll()
    {
        var oldData = State.Items;
        State.Items = new List<BookMarkMetaItem>()
        {
            new()
            {
                GrainIndex = 1
            }
        };
        return oldData;
    }
}
using CAServer.Grains.State.Bookmark;
using Google.Protobuf.WellKnownTypes;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Bookmark;

public class BookmarkMetaGrain : Grain<BookmarkMetaState>, IBookmarkMetaGrain
{
    private const int GrainSize = 100;
    private readonly IObjectMapper _objectMapper;

    public BookmarkMetaGrain(IObjectMapper objectMapper)
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

    public async Task<int> GetTailBookMarkGrainIndex()
    {
        if (State.Items.IsNullOrEmpty())
        {
            State.Items.Add(new BookMarkMetaItem() { Index = 1 });
            await WriteStateAsync();
            return 1;
        }

        var itemDict = State.Items.ToDictionary(i => i.Index, i => i);
        var hasDataList = State.Items.Where(item => item.Size > 0).ToList();
        if (hasDataList.IsNullOrEmpty())
            return 1;

        var tail = hasDataList[hasDataList.Count - 1];
        if (tail.Size < GrainSize)
            return tail.Index;

        var idx = tail.Index + 1;
        if (!itemDict.ContainsKey(idx))
        {
            State.Items.Add(new BookMarkMetaItem() { Index = idx });
            await WriteStateAsync();
        }

        return idx;
    }

    public async Task<List<BookMarkMetaItem>> RemoveAll()
    {
        var oldData = State.Items;
        State.Items = new List<BookMarkMetaItem>()
        {
            new()
            {
                Index = 1
            }
        };
        await WriteStateAsync();
        return oldData;
    }

    public async Task<Empty> UpdateGrainIndexCount(Dictionary<int, int> indexCountDict)
    {
        var itemDict = State.Items.ToDictionary(i => i.Index, i => i);
        foreach (var indexCount in indexCountDict)
        {
            if (!itemDict.ContainsKey(indexCount.Key))
                continue;
            itemDict.GetValueOrDefault(indexCount.Key).Size = indexCount.Value;
        }

        await WriteStateAsync();
        return null;
    }

    public async Task<List<Tuple<int, int>>> GetIndexCount()
    {
        return State
            .Items.Where(item => item.Size > 0)
            .Select(item => new Tuple<int, int>(item.Index, item.Size))
            .ToList();
    }
}
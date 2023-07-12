using CAServer.Grains.State.Bookmark;
using Google.Protobuf.WellKnownTypes;
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

    public async Task<int> GetTailBookMarkGrainIndex()
    {
        if (State.Items.IsNullOrEmpty())
        {
            State.Items.Add(new BookMarkMetaItem() { GrainIndex = 1 });
            await WriteStateAsync();
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
            await WriteStateAsync();
            return idx;
        }
        return tail.GrainIndex;
    }

    public async Task<List<BookMarkMetaItem>> RemoveAll()
    {
        var oldData = State.Items;
        State.Items = new List<BookMarkMetaItem>()
        {
            new()
            {
                GrainIndex = 1
            }
        };
        await WriteStateAsync();
        return oldData;
    }

    public async Task<Empty> UpdateGrainIndexCount(Dictionary<int, int> indexCountDict)
    { 
        var itemDict = State.Items.ToDictionary(i => i.GrainIndex, i => i);
        foreach (var indexCount in indexCountDict)
        {
            if (!itemDict.ContainsKey(indexCount.Key))
                continue;
            itemDict.GetValueOrDefault(indexCount.Key).Size = indexCount.Value;
        }
        await WriteStateAsync();
        return null;
    }
}
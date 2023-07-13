using CAServer.Grains.State.Bookmark;
using Google.Protobuf.WellKnownTypes;
using Orleans;

namespace CAServer.Grains.Grain.Bookmark;

public interface IBookmarkMetaGrain : IGrainWithStringKey
{
    Task<int> GetTailBookMarkGrainIndex();

    Task<List<BookMarkMetaItem>> RemoveAll();

    Task<Empty> UpdateGrainIndexCount(Dictionary<int, int> indexCountDict);

    Task<List<Tuple<int, int>>> GetIndexCount();
}
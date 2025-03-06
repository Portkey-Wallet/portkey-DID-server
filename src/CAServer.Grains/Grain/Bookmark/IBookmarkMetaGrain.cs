using CAServer.Grains.State.Bookmark;

namespace CAServer.Grains.Grain.Bookmark;

public interface IBookmarkMetaGrain : IGrainWithStringKey
{
    Task<int> GetTailBookMarkGrainIndex();

    Task<List<BookMarkMetaItem>> RemoveAll();

    Task<string> UpdateGrainIndexCount(Dictionary<int, int> indexCountDict);

    Task<List<Tuple<int, int>>> GetIndexCount();
}
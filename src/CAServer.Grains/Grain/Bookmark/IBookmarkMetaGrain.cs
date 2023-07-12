using CAServer.Grains.State.Bookmark;
using Google.Protobuf.WellKnownTypes;
using JetBrains.Annotations;
using Orleans;

namespace CAServer.Grains.Grain.Bookmark;

public interface IBookmarkMetaGrain: IGrainWithStringKey
{

    Task<int> GetTailBookMarkGrainIndexAsync();

    Task<List<BookMarkMetaItem>> RemoveAllAsync();

    Task<Empty> UpdateGrainIndexCountAsync(Dictionary<int, int> indexCountDict);

}
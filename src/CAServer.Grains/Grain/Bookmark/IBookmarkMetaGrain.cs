using CAServer.Grains.State.Bookmark;
using Orleans;

namespace CAServer.Grains.Grain.Bookmark;

public interface IBookmarkMetaGrain: IGrainWithStringKey
{

    int GetTailBookMarkGrainIndex();

    List<BookMarkMetaItem> RemoveAll();


}
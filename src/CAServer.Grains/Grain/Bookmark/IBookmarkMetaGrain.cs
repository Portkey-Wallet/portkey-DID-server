using Orleans;

namespace CAServer.Grains.Grain.Bookmark;

public interface IBookmarkMetaGrain: IGrainWithStringKey
{

    int GetTailBookMarkGrainIndex();


}
using Orleans;

namespace CAServer.Grains.Grain.ApplicationHandler;

public interface ISortNumGrain : IGrainWithStringKey
{
    Task<int>  GetSortNum(int limit);
}
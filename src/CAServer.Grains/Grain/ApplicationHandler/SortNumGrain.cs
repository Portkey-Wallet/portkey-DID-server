using CAServer.Grains.State.ApplicationHandler;
using Orleans;

namespace CAServer.Grains.Grain.ApplicationHandler;

public class SortNumGrain  : Grain<SortNumState>, ISortNumGrain
{
    public async Task SetStateAsync(long height)
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public async Task GetStateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<int> GetSortNum(int limit)
    {
       
        if (string.IsNullOrWhiteSpace(State.Id))
        {
            State.Id = this.GetPrimaryKeyString();
        }

        if (State.ResetTime < DateTime.Today )
        {
            State.ResetTime = DateTime.Now;
            State.SortNum = 0;
        }
        else
        {
            State.SortNum += 1 ;
        }
        await WriteStateAsync();
        return (int)(State.SortNum % limit);
    }
}
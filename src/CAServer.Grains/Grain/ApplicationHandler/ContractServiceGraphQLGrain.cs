using CAServer.Grains.State.ApplicationHandler;
using Orleans;

namespace CAServer.Grains.Grain.ApplicationHandler;

public class ContractServiceGraphQLGrain : Grain<GraphQlState>, IContractServiceGraphQLGrain
{
    public override Task OnActivateAsync()
    {
        ReadStateAsync();
        return base.OnActivateAsync();
    }

    public async Task SetStateAsync(long height)
    {
        if (height < State.EndHeight)
        {
            return;
        }
        State.EndHeight = height;
        await WriteStateAsync();
    }

    public Task<long> GetStateAsync()
    {
        return Task.FromResult(State.EndHeight);
    }
}
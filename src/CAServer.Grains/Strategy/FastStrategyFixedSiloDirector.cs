using Orleans.Runtime;
using Orleans.Runtime.Placement;

namespace CAServer.Grains.Strategy;

public class FastStrategyFixedSiloDirector : IPlacementDirector

{
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var silos = context.GetCompatibleSilos(target).OrderBy(s => s).ToArray();
        return Task.FromResult(silos[0]);
    }
}
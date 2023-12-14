using Orleans.Placement;
using Orleans.Runtime;

namespace CAServer.Grains.Strategy;
[Serializable]
public class FastStrategy :  PlacementStrategy
{
    
}
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class FastStrategyAttribute : PlacementAttribute
{
    public FastStrategyAttribute() :
        base(new FastStrategy())
    {
    }
}
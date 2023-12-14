using Orleans.Placement;
using Orleans.Runtime;

namespace CAServer.Grains.Strategy;
[Serializable]
public class SlowStrategy :  PlacementStrategy
{
  
}
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class SlowStrategyAttribute : PlacementAttribute
{
    public SlowStrategyAttribute() :
        base(new SlowStrategy())
    {
    }
}
using System.Collections.Generic;
using System.Linq;
using CAServer.Common;
using CAServer.ThirdPart.Processors;

namespace CAServer.ThirdPart.Processor;

public class ThirdPartNftOrderProcessorFactory : IThirdPartOrderProcessorFactory
{
    private readonly Dictionary<string, IThirdPartNftOrderProcessor> _processors;


    public ThirdPartNftOrderProcessorFactory(IEnumerable<IThirdPartNftOrderProcessor> processors)
    {
        _processors = processors.ToDictionary(processor => processor.ThirdPartName(), processor => processor);
    }

    public IThirdPartNftOrderProcessor GetProcessor(string thirdPartName)
    {
        AssertHelper.IsTrue(_processors.ContainsKey(thirdPartName), "Processor of {Name} not found", thirdPartName);
        return _processors.GetValueOrDefault(thirdPartName);
    }
}
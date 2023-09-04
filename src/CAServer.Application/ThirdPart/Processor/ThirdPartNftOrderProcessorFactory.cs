using System.Collections.Generic;
using System.Linq;
using CAServer.Common;
using CAServer.ThirdPart.Processors;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Processor;

public class ThirdPartNftOrderProcessorFactory : IThirdPartNftOrderProcessorFactory, ISingletonDependency
{
    private readonly Dictionary<string, IThirdPartNftOrderProcessor> _processors;


    public ThirdPartNftOrderProcessorFactory(IEnumerable<IThirdPartNftOrderProcessor> processors)
    {
        _processors = processors.ToDictionary(processor => processor.ThirdPartName().ToLower(), processor => processor);
    }

    public IThirdPartNftOrderProcessor GetProcessor(string thirdPartName)
    {
        var processorName = thirdPartName.ToLower();
        AssertHelper.IsTrue(_processors.ContainsKey(processorName), "Processor of {Name} not found", thirdPartName);
        return _processors.GetValueOrDefault(processorName);
    }
}
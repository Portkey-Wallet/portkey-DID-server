using System.Collections.Generic;
using System.Linq;
using CAServer.Common;
using CAServer.ThirdPart.Processors;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart;

public class TreasuryProcessorFactory : ITreasuryProcessorFactory, ITransientDependency
{
    
    private readonly Dictionary<string, IThirdPartTreasuryProcessor> _treasuryProcessors;

    public TreasuryProcessorFactory(IEnumerable<IThirdPartTreasuryProcessor> treasuryProcessors)
    {
        _treasuryProcessors = treasuryProcessors.ToDictionary(p => p.ThirdPartName().ToString());
    }

    public IThirdPartTreasuryProcessor Processor(string thirdPartName)
    {
        var processorExists = _treasuryProcessors.TryGetValue(ThirdPartNameType.Alchemy.ToString(), out var processor);
        AssertHelper.IsTrue(processorExists, "Treasury processor not found {}", thirdPartName);
        return processor;
    }
}
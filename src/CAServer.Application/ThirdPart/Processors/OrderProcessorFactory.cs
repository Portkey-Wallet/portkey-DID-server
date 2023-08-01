using System.Collections.Generic;
using System.Linq;
using Volo.Abp;

namespace CAServer.ThirdPart.Processors;

public class OrderProcessorFactory : IOrderProcessorFactory
{
    private IEnumerable<IOrderProcessor> _processors;

    public OrderProcessorFactory(List<IOrderProcessor> processors)
    {
        _processors = processors;
    }

    public IOrderProcessor GetProcessor(string merchantName)
    {
        var processor = _processors.FirstOrDefault(p => p.MerchantName() == merchantName, null);
        if (processor == null) throw new UserFriendlyException($"not support merchant {merchantName}");
        return processor;
    }
    
}
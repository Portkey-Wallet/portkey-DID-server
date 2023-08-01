using System.Collections.Generic;
using System.Linq;

namespace CAServer.ThirdPart.Processors;

public class OrderProcessorFactory
{
    private IEnumerable<AbstractOrderProcessor> _processors;

    public OrderProcessorFactory(List<AbstractOrderProcessor> processors)
    {
        _processors = processors;
    }

    public AbstractOrderProcessor GetProcessor(string merchantName)
    {
        return _processors.FirstOrDefault(p => p.MerchantName() == merchantName, null);
    }
    
}
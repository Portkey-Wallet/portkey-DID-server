using System;
using System.Collections.Generic;
using System.Linq;
using CAServer.ThirdPart.Dtos;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Processors;

public class OrderProcessorFactory : IOrderProcessorFactory, ISingletonDependency
{
    private IEnumerable<IOrderProcessor> _processors;

    public OrderProcessorFactory(IEnumerable<IOrderProcessor> processors)
    {
        _processors = processors;
    }

    public IOrderProcessor GetProcessor(string merchantName)
    {
        if (ThirdPartHelper.MerchantNameExist(merchantName) == MerchantNameType.Unknown)
            throw new UserFriendlyException($"not support merchant {merchantName} !");
        var processor = _processors.FirstOrDefault(p
            => String.Equals(p.MerchantName(), merchantName, StringComparison.CurrentCultureIgnoreCase), null);
        if (processor == null) throw new UserFriendlyException($"not support merchant {merchantName}");
        return processor;
    }
    
}
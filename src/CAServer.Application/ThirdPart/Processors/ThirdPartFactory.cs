using System;
using System.Collections.Generic;
using System.Linq;
using CAServer.ThirdPart.Dtos;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Processors;

public class ThirdPartFactory : IThirdPartFactory, ISingletonDependency
{
    private readonly Dictionary<string, IOrderProcessor> _processors;
    private readonly Dictionary<string, IThirdPartAppService> _thirdPartAppServices;

    public ThirdPartFactory(IEnumerable<IOrderProcessor> processors, IEnumerable<IThirdPartAppService> thirdPartAppServices)
    {
        _processors = processors.ToDictionary(p => p.MerchantName().ToLower(), processor => processor);
        _thirdPartAppServices = thirdPartAppServices.ToDictionary(s => s.MerchantName().ToLower(), s => s);
    }

    public IOrderProcessor GetProcessor(string merchantName)
    {
        if (ThirdPartHelper.MerchantNameExist(merchantName) == MerchantNameType.Unknown)
            throw new UserFriendlyException($"not support merchant {merchantName} !");
        var processor = _processors.GetValueOrDefault(merchantName.ToLower(), null);
        if (processor == null) throw new UserFriendlyException($"not support merchant {merchantName}");
        return processor;
    }

    public IThirdPartAppService GetAppService(string merchantName)
    {
        if (ThirdPartHelper.MerchantNameExist(merchantName) == MerchantNameType.Unknown)
            throw new UserFriendlyException($"not support merchant {merchantName} !");
        var processor = _thirdPartAppServices.GetValueOrDefault(merchantName.ToLower(), null);
        if (processor == null) throw new UserFriendlyException($"not support merchant {merchantName}");
        return processor;
    }
}
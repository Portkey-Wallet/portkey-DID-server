using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace CAServer.IpWhiteList;

public class IpWhiteListAppService : IIpWhiteListAppService,ISingletonDependency
{
    
    public Task<List<string>> GetIpWhiteListAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task<bool> IsInWhiteListAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task AddIpWhiteListAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task RemoveIpWhiteListAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task UpdateIpWhiteListAsync()
    {
        throw new System.NotImplementedException();
    }
}
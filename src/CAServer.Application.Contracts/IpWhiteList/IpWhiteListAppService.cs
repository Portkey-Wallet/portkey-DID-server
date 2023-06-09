using System.Collections.Generic;
using System.Threading.Tasks;

namespace CAServer.IpWhiteList;

public interface IIpWhiteListAppService
{
    Task<List<string>> GetIpWhiteListAsync();
    
    Task<bool> IsInWhiteListAsync();
    
    Task AddIpWhiteListAsync();
    
    Task RemoveIpWhiteListAsync();
    
    Task UpdateIpWhiteListAsync();



}
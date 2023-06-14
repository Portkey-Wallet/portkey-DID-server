using System.Collections.Generic;
using System.Threading.Tasks;
using CAVerifierServer.IpWhiteList;

namespace CAServer.IpWhiteList;

public interface IIpWhiteListAppService
{
    Task<List<string>> GetIpWhiteListAsync();
    
    Task<bool> IsInWhiteListAsync(string userIpAddress);
    
    Task AddIpWhiteListAsync(AddUserIpToWhiteListRequestDto requestDto);
    
    Task RemoveIpWhiteListAsync();
    
    Task UpdateIpWhiteListAsync();



}
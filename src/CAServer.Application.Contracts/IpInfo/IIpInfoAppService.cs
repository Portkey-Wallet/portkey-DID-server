using System.Threading.Tasks;

namespace CAServer.IpInfo;

public interface IIpInfoAppService
{
    Task<IpInfoResultDto> GetIpInfoAsync();

    string GetRemoteIp();
    
    string GetRemoteIp(string random);
}
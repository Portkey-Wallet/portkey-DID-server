using System.Threading.Tasks;

namespace CAServer.IpInfo;

public interface IIpInfoAppService
{
    Task<IpInfoResultDto> GetIpInfoAsync();
}
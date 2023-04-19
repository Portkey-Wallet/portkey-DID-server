using System.Threading.Tasks;

namespace CAServer.IpInfo;

public interface IIpInfoClient
{
    Task<IpInfoDto> GetIpInfoAsync(string ip);
}
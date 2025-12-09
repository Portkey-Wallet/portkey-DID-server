using System.Threading.Tasks;

namespace CAServer.IpInfo;

public interface IIpInfoClient
{
    Task<IpInfoDto> GetIpInfoAsync(string ip);
    Task<IpInfoDto> GetCountryInfoAsync(string ip);
}
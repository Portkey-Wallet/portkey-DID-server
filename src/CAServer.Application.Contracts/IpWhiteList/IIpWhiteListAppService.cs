using System.Threading.Tasks;
using CAServer.IpWhiteList.Dtos;

namespace CAServer.IpWhiteList;

public interface IIpWhiteListAppService
{
    Task<bool> IsInWhiteListAsync(string userIpAddress);

    Task AddIpWhiteListAsync(AddUserIpToWhiteListRequestDto requestDto);
}
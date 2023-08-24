using System.Threading.Tasks;
using CAServer.Security.Dtos;

namespace CAServer.Security;

public interface IUserSecurityAppService
{
    public Task<UserSecuritySelfTestResultDto> GetUserSecuritySelfTestAsync(GetUserSecuritySelfTestDto input);
}
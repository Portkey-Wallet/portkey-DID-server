using System.Threading.Tasks;
using CAServer.Security;
using CAServer.Security.Dtos;

namespace CAServer.UserSecurityAppService;

public class UserSecurityAppService : CAServerAppService, IUserSecurityAppService
{
    public async Task<UserSecuritySelfTestResultDto> GetUserSecuritySelfTestAsync(GetUserSecuritySelfTestDto input)
    {
        return new UserSecuritySelfTestResultDto
        {
            SocialRecovery = false,
            ModifyGuardian = false,
            RemoveDevice = false,
            ModifyTransferLimit = false,
            Approve = false,
            ModifyStrategy = false
        };
    }
}
using System.Threading.Tasks;
using CAServer.UserExtraInfo.Dtos;

namespace CAServer.AppleAuth.Provider;

public interface IAppleUserProvider
{
    Task SetUserExtraInfoAsync(AppleUserExtraInfo userExtraInfo);

    Task<AppleUserExtraInfo> GetUserExtraInfoAsync(string userId);

    Task<bool> UserExtraInfoExistAsync(string userId);

    Task<UserExtraInfoResultDto> GetUserInfoAsync(string userId);
}
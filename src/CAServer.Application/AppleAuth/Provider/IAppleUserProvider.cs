using System.Threading.Tasks;

namespace CAServer.AppleAuth.Provider;

public interface IAppleUserProvider
{
    Task SetUserExtraInfoAsync(AppleUserExtraInfo userExtraInfo);

    Task<AppleUserExtraInfo> GetUserExtraInfoAsync(string userId);

    Task<bool> UserExtraInfoExistAsync(string userId);
}
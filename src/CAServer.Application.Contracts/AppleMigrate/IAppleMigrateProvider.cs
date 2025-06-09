using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.AppleMigrate.Dtos;

namespace CAServer.AppleMigrate;

public interface IAppleMigrateProvider
{
    Task<Dictionary<string, string>> GetSecretAndAccessToken();
    Task<string> GetAccessToken(string clientId, string clientSecret);
    Task<GetSubDto> GetSubAsync(string userId);
    Task<AppleUserTransferInfo> GetTransferInfoFromCache(string userId);
    Task<GetNewUserIdDto> GetNewUserIdAsync(string transferSub);

    Task<int> SetTransferSubAsync();
    Task<int> SetNewUserInfoAsync();
}
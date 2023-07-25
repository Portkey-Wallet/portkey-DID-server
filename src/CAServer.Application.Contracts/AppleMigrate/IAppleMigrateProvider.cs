using System.Threading.Tasks;
using CAServer.AppleMigrate.Dtos;

namespace CAServer.AppleMigrate;

public interface IAppleMigrateProvider
{
    string GetSecret();
    Task<string> GetAccessToken(string clientId, string clientSecret);
    Task<GetSubDto> GetSubAsync(string userId);
    Task<GetNewUserIdDto> GetNewUserIdAsync(string transferSub);
}
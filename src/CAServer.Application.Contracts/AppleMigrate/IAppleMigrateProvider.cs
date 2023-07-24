using System.Threading.Tasks;
using CAServer.AppleMigrate.Dtos;

namespace CAServer.AppleMigrate;

public interface IAppleMigrateProvider
{
    string GetSecret();
    Task<string> GetAccessToken();
    Task<GetSubDto> GetSubAsync(string userId);
    Task<GetNewUserIdDto> GetNewUserIdAsync(string userId);
}
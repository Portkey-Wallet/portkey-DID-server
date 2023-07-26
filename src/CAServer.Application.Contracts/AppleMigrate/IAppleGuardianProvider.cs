using System.Threading.Tasks;

namespace CAServer.AppleMigrate;

public interface IAppleGuardianProvider
{
    Task<int> SetAppleGuardianIntoCache();
    Task<AppleUserTransfer> GetAppleGuardianIntoCache();
    Task<object> GetMigrateResult(string userId);
}
using System.Threading.Tasks;

namespace CAServer.AppleMigrate;

public interface IAppleGuardianProvider
{
    Task<int> SetAppleGuardianIntoCache();
}
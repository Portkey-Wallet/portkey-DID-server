using System.Threading.Tasks;
using CAServer.Verifier;

namespace CAServer.Google;

public interface IGoogleAppService
{
    Task<bool> IsGoogleRecaptchaOpenAsync(string userIpAddress, OperationType type);
    Task<bool> IsGoogleRecaptchaTokenValidAsync(string recaptchatoken, PlatformType platformType = PlatformType.WEB);
    Task<bool> GoogleRecaptchaV3Async(string inputRecaptchaToken, PlatformType inputPlatformType = PlatformType.WEB);
}
using System.Threading.Tasks;
using CAServer.Google.Dtos;
using CAServer.Verifier;

namespace CAServer.Google;

public interface IGoogleAppService
{
    Task<bool> IsGoogleRecaptchaOpenAsync(string userIpAddress, OperationType type);
    Task<bool> IsGoogleRecaptchaTokenValidAsync(string recaptchatoken, PlatformType platformType = PlatformType.WEB);
    Task<ValidateTokenResponse> ValidateTokenAsync(string rcToken, string acToken, PlatformType platformType = PlatformType.WEB);

}
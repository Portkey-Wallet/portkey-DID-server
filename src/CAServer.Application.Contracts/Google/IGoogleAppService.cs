using System.Threading.Tasks;
using CAServer.AppleAuth.Dtos;
using CAServer.Google.Dtos;

namespace CAServer.Google;

public interface IGoogleAppService
{
    Task<bool> IsGoogleRecaptchaOpenAsync(string userIpAddress);
    Task<bool> IsGoogleRecaptchaTokenValidAsync(string recaptchatoken);
    Task<string> ReceiveAsync(GoogleAuthDto appleAuthDto);
}
using System.Threading.Tasks;

namespace CAServer.Google;

public interface IGoogleAppService
{
    Task<bool> IsGoogleRecaptchaOpenAsync(string userIpAddress);
    Task<bool> IsGoogleRecaptchaTokenValidAsync(string recaptchatoken);
}
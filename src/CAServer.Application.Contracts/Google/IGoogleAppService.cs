using System.Threading.Tasks;
using CAServer.Verifier;

namespace CAServer.Google;

public interface IGoogleAppService
{
    Task<bool> IsGoogleRecaptchaOpenAsync(string userIpAddress, OperationType type, string version);
    Task<bool> IsGoogleRecaptchaTokenValidAsync(string recaptchatoken);
}
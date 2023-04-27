using System.Threading.Tasks;

namespace CAServer.Google;

public interface IGoogleAppService
{
    public Task<bool> IsGoogleRecaptchaOpen(string userIpAddress);
    Task<bool> GoogleRecaptchaTokenSuccessAsync(string recaptchatoken);
    
}
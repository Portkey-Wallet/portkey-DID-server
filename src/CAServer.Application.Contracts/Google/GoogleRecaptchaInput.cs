using CAServer.Verifier;

namespace CAServer.Google;

public class GoogleRecaptchaInput
{
    public string RecaptchaToken { get; set; }
    
    public PlatformType PlatformType { get; set; }
    
    
}
using System.Collections.Generic;

namespace CAServer.Options;

public class GoogleRecaptchaOptions
{
    public string Secret { get; set; }

    public string VerifyUrl { get; set; }

    public List<string> RecaptchaUrls { get;set; }

}
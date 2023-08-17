using System.Collections.Generic;

namespace CAServer.Options;

public class GoogleRecaptchaOptions
{
    public Dictionary<string, string> SecretMap { get; set; }

    public string VerifyUrl { get; set; }
}
using System.Collections.Generic;

namespace CAServer.Options;

public class GoogleRecaptchaOptions
{
    public Dictionary<string, string> SecretMap { get; set; }
    public Dictionary<string, string> V3SecretMap { get; set; }
    public string VerifyUrl { get; set; }

    public float Score { get; set; }
}
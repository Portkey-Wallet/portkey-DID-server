namespace CAServer.Options;

public class RegistrationEmailRateLimitOptions
{
    public bool IsEnabled { get; set; }

    public RegistrationEmailRateLimitRuleOptions CreateCAHolder { get; set; } = new()
    {
        Per10Minutes = 10,
        PerHour = 30
    };

    public RegistrationEmailRateLimitRuleOptions SocialRecovery { get; set; } = new()
    {
        Per10Minutes = 15,
        PerHour = 45
    };
}

public class RegistrationEmailRateLimitRuleOptions
{
    public int Per10Minutes { get; set; }

    public int PerHour { get; set; }
}

using System.Collections.Generic;
using CAServer.Verifier;

namespace CAServer.Options;

public class RegistrationEmailRateLimitOptions
{
    public bool IsEnabled { get; set; }

    public Dictionary<OperationType, RegistrationEmailRateLimitPolicyOptions> Policies { get; set; } = new();
}

public class RegistrationEmailRateLimitPolicyOptions
{
    public string GuardianType { get; set; }

    public int Per10Minutes { get; set; }

    public int PerHour { get; set; }

    public bool RequireGuardianExistsBeforeConsume { get; set; }
}

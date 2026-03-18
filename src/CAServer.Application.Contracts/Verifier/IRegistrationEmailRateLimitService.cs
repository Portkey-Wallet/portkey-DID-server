using System.Threading.Tasks;

namespace CAServer.Verifier;

public interface IRegistrationEmailRateLimitService
{
    RegistrationEmailRateLimitPolicy GetPolicy(RegistrationEmailRateLimitContext context);

    Task<RegistrationEmailRateLimitCheckResult> CheckAsync(RegistrationEmailRateLimitContext context);
}

public class RegistrationEmailRateLimitContext
{
    public string GuardianType { get; set; }

    public OperationType OperationType { get; set; }

    public string ClientIp { get; set; }

    public string TraceId { get; set; }
}

public class RegistrationEmailRateLimitPolicy
{
    public string GuardianType { get; set; }

    public OperationType OperationType { get; set; }

    public int Per10Minutes { get; set; }

    public int PerHour { get; set; }

    public bool RequireGuardianExistsBeforeConsume { get; set; }

    public bool HasEffectiveWindow => Per10Minutes > 0 || PerHour > 0;
}

public class RegistrationEmailRateLimitCheckResult
{
    public bool IsAllowed { get; set; }

    public RegistrationEmailRateLimitPolicy Policy { get; set; }

    public string WindowName { get; set; }

    public int Limit { get; set; }

    public long Count { get; set; }

    public int RemainingQuota { get; set; }

    public int RetryAfterSeconds { get; set; }

    public static RegistrationEmailRateLimitCheckResult Allow()
    {
        return new RegistrationEmailRateLimitCheckResult
        {
            IsAllowed = true
        };
    }

    public static RegistrationEmailRateLimitCheckResult Block(RegistrationEmailRateLimitPolicy policy,
        string windowName, int limit, long count, int remainingQuota, int retryAfterSeconds)
    {
        return new RegistrationEmailRateLimitCheckResult
        {
            IsAllowed = false,
            Policy = policy,
            WindowName = windowName,
            Limit = limit,
            Count = count,
            RemainingQuota = remainingQuota,
            RetryAfterSeconds = retryAfterSeconds
        };
    }
}

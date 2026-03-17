using System.Threading.Tasks;

namespace CAServer.Verifier;

public interface IRegistrationEmailRateLimitService
{
    bool ShouldApply(string guardianType, OperationType operationType);

    Task<RegistrationEmailRateLimitCheckResult> CheckAsync(string clientIp, OperationType operationType,
        string traceId);
}

public class RegistrationEmailRateLimitCheckResult
{
    public bool IsAllowed { get; set; }

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

    public static RegistrationEmailRateLimitCheckResult Block(string windowName, int limit, long count,
        int remainingQuota, int retryAfterSeconds)
    {
        return new RegistrationEmailRateLimitCheckResult
        {
            IsAllowed = false,
            WindowName = windowName,
            Limit = limit,
            Count = count,
            RemainingQuota = remainingQuota,
            RetryAfterSeconds = retryAfterSeconds
        };
    }
}

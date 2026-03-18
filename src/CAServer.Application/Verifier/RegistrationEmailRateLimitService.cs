using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Cache;
using CAServer.CAAccount.Dtos;
using CAServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.Verifier;

public class RegistrationEmailRateLimitService : IRegistrationEmailRateLimitService, ITransientDependency
{
    private const string CacheKeyPrefix = "RegistrationEmailRateLimit";
    private static readonly TimeSpan TenMinuteWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan HourWindow = TimeSpan.FromHours(1);
    private readonly ICacheProvider _cacheProvider;
    private readonly ILogger<RegistrationEmailRateLimitService> _logger;
    private readonly RegistrationEmailRateLimitOptions _options;

    public RegistrationEmailRateLimitService(ICacheProvider cacheProvider,
        ILogger<RegistrationEmailRateLimitService> logger,
        IOptionsSnapshot<RegistrationEmailRateLimitOptions> options)
    {
        _cacheProvider = cacheProvider;
        _logger = logger;
        _options = options.Value;
    }

    public RegistrationEmailRateLimitPolicy GetPolicy(RegistrationEmailRateLimitContext context)
    {
        if (!_options.IsEnabled || context == null ||
            !TryGetPolicyOptions(context.OperationType, out var policyOptions))
        {
            return null;
        }

        if (!TryParseGuardianType(context.GuardianType, out var contextGuardianType) ||
            !TryParseGuardianType(policyOptions.GuardianType, out var policyGuardianType) ||
            contextGuardianType != policyGuardianType)
        {
            return null;
        }

        var policy = new RegistrationEmailRateLimitPolicy
        {
            GuardianType = policyGuardianType.ToString(),
            OperationType = context.OperationType,
            Per10Minutes = policyOptions.Per10Minutes,
            PerHour = policyOptions.PerHour,
            RequireGuardianExistsBeforeConsume = policyOptions.RequireGuardianExistsBeforeConsume
        };

        return policy.HasEffectiveWindow ? policy : null;
    }

    public async Task<RegistrationEmailRateLimitCheckResult> CheckAsync(RegistrationEmailRateLimitContext context)
    {
        var policy = GetPolicy(context);
        if (policy == null)
        {
            return RegistrationEmailRateLimitCheckResult.Allow();
        }

        if (string.IsNullOrWhiteSpace(context.ClientIp))
        {
            throw new ArgumentException("ClientIp is required when registration email rate limit policy applies.",
                nameof(context));
        }

        var clientIp = context.ClientIp;
        var traceId = context.TraceId;
        var now = GetUtcNow();
        RegistrationEmailRateLimitCheckResult blockedResult = null;
        foreach (var window in BuildWindows(policy))
        {
            try
            {
                var windowStart = GetWindowStart(now, window.WindowSize);
                var windowEnd = windowStart.Add(window.WindowSize);
                var ttl = windowEnd - now;
                if (ttl <= TimeSpan.Zero)
                {
                    ttl = TimeSpan.FromSeconds(1);
                }

                var key = $"{CacheKeyPrefix}:{policy.OperationType}:{window.Name}:{windowStart:yyyyMMddHHmm}:{clientIp}";
                var count = await _cacheProvider.Increase(key, 1, ttl);
                var remainingQuota = Math.Max(window.Limit - (int)count, 0);

                if (count > window.Limit)
                {
                    var retryAfterSeconds = (int)Math.Ceiling(ttl.TotalSeconds);
                    _logger.LogWarning(
                        "Registration email rate limit exceeded. traceId:{TraceId}, ip:{Ip}, operationType:{OperationType}, window:{Window}, limit:{Limit}, count:{Count}, remaining:{RemainingQuota}, retryAfterSeconds:{RetryAfterSeconds}",
                        traceId, clientIp, policy.OperationType, window.Name, window.Limit, count, remainingQuota,
                        retryAfterSeconds);
                    if (blockedResult == null || retryAfterSeconds > blockedResult.RetryAfterSeconds)
                    {
                        blockedResult = RegistrationEmailRateLimitCheckResult.Block(policy, window.Name, window.Limit,
                            count, remainingQuota, retryAfterSeconds);
                    }

                    continue;
                }

                _logger.LogDebug(
                    "Registration email rate limit check passed. traceId:{TraceId}, ip:{Ip}, operationType:{OperationType}, window:{Window}, limit:{Limit}, count:{Count}, remaining:{RemainingQuota}",
                    traceId, clientIp, policy.OperationType, window.Name, window.Limit, count, remainingQuota);
            }
            catch (Exception e)
            {
                if (blockedResult != null)
                {
                    _logger.LogError(e,
                        "Registration email rate limit preserved existing block after window check failure. traceId:{TraceId}, ip:{Ip}, operationType:{OperationType}, window:{Window}",
                        traceId, clientIp, policy.OperationType, window.Name);
                    return blockedResult;
                }

                _logger.LogError(e,
                    "Registration email rate limit failed open. traceId:{TraceId}, ip:{Ip}, operationType:{OperationType}, window:{Window}",
                    traceId, clientIp, policy.OperationType, window.Name);
                return RegistrationEmailRateLimitCheckResult.Allow();
            }
        }

        return blockedResult ?? RegistrationEmailRateLimitCheckResult.Allow();
    }

    protected virtual DateTime GetUtcNow()
    {
        return DateTime.UtcNow;
    }

    private bool TryGetPolicyOptions(OperationType operationType, out RegistrationEmailRateLimitPolicyOptions policy)
    {
        policy = null;
        return _options.Policies != null && _options.Policies.TryGetValue(operationType, out policy) &&
               policy != null;
    }

    private static bool TryParseGuardianType(string guardianType, out GuardianIdentifierType parsedGuardianType)
    {
        return RegistrationEmailRateLimitGuardianTypeHelper.TryParseDefinedGuardianType(guardianType,
            out parsedGuardianType);
    }

    private static IEnumerable<RateLimitWindow> BuildWindows(RegistrationEmailRateLimitPolicy policy)
    {
        if (policy.Per10Minutes > 0)
        {
            yield return new RateLimitWindow($"{policy.OperationType}:10m", policy.Per10Minutes, TenMinuteWindow);
        }

        if (policy.PerHour > 0)
        {
            yield return new RateLimitWindow($"{policy.OperationType}:1h", policy.PerHour, HourWindow);
        }
    }

    private static DateTime GetWindowStart(DateTime now, TimeSpan windowSize)
    {
        if (windowSize == TenMinuteWindow)
        {
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute / 10 * 10, 0,
                DateTimeKind.Utc);
        }

        if (windowSize == HourWindow)
        {
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        }

        throw new ArgumentOutOfRangeException(nameof(windowSize), windowSize, "Unsupported window size.");
    }

    private sealed class RateLimitWindow
    {
        public RateLimitWindow(string name, int limit, TimeSpan windowSize)
        {
            Name = name;
            Limit = limit;
            WindowSize = windowSize;
        }

        public string Name { get; }

        public int Limit { get; }

        public TimeSpan WindowSize { get; }
    }
}

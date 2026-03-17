using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Cache;
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

    public bool ShouldApply(string guardianType, OperationType operationType)
    {
        return _options.IsEnabled &&
               string.Equals(guardianType, "Email", StringComparison.OrdinalIgnoreCase) &&
               TryGetRule(operationType, out var rule) &&
               HasEffectiveWindow(rule);
    }

    public async Task<RegistrationEmailRateLimitCheckResult> CheckAsync(string clientIp, OperationType operationType,
        string traceId)
    {
        if (!_options.IsEnabled || !TryGetRule(operationType, out var rule) || !HasEffectiveWindow(rule))
        {
            return RegistrationEmailRateLimitCheckResult.Allow();
        }

        try
        {
            var now = GetUtcNow();
            foreach (var window in BuildWindows(operationType, rule))
            {
                var windowStart = GetWindowStart(now, window.WindowSize);
                var windowEnd = windowStart.Add(window.WindowSize);
                var ttl = windowEnd - now;
                if (ttl <= TimeSpan.Zero)
                {
                    ttl = TimeSpan.FromSeconds(1);
                }

                var key = $"{CacheKeyPrefix}:{operationType}:{window.Name}:{windowStart:yyyyMMddHHmm}:{clientIp}";
                var count = await _cacheProvider.Increase(key, 1, ttl);
                var remainingQuota = Math.Max(window.Limit - (int)count, 0);

                if (count > window.Limit)
                {
                    var retryAfterSeconds = (int)Math.Ceiling(ttl.TotalSeconds);
                    _logger.LogWarning(
                        "Registration email rate limit exceeded. traceId:{TraceId}, ip:{Ip}, operationType:{OperationType}, window:{Window}, limit:{Limit}, count:{Count}, remaining:{RemainingQuota}, retryAfterSeconds:{RetryAfterSeconds}",
                        traceId, clientIp, operationType, window.Name, window.Limit, count, remainingQuota,
                        retryAfterSeconds);
                    return RegistrationEmailRateLimitCheckResult.Block(window.Name, window.Limit, count,
                        remainingQuota, retryAfterSeconds);
                }

                _logger.LogDebug(
                    "Registration email rate limit check passed. traceId:{TraceId}, ip:{Ip}, operationType:{OperationType}, window:{Window}, limit:{Limit}, count:{Count}, remaining:{RemainingQuota}",
                    traceId, clientIp, operationType, window.Name, window.Limit, count, remainingQuota);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Registration email rate limit failed open. traceId:{TraceId}, ip:{Ip}, operationType:{OperationType}",
                traceId, clientIp, operationType);
        }

        return RegistrationEmailRateLimitCheckResult.Allow();
    }

    private bool TryGetRule(OperationType operationType, out RegistrationEmailRateLimitRuleOptions rule)
    {
        rule = operationType switch
        {
            OperationType.CreateCAHolder => _options.CreateCAHolder,
            OperationType.SocialRecovery => _options.SocialRecovery,
            _ => null
        };

        return rule != null;
    }

    protected virtual DateTime GetUtcNow()
    {
        return DateTime.UtcNow;
    }

    private static bool HasEffectiveWindow(RegistrationEmailRateLimitRuleOptions rule)
    {
        return rule is { Per10Minutes: > 0 } || rule is { PerHour: > 0 };
    }

    private static IEnumerable<RateLimitWindow> BuildWindows(OperationType operationType,
        RegistrationEmailRateLimitRuleOptions rule)
    {
        if (rule.Per10Minutes > 0)
        {
            yield return new RateLimitWindow($"{operationType}:10m", rule.Per10Minutes, TenMinuteWindow);
        }

        if (rule.PerHour > 0)
        {
            yield return new RateLimitWindow($"{operationType}:1h", rule.PerHour, HourWindow);
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

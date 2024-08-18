using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.Telegram;

public class TelegramRateLimiter : ITelegramRateLimiter, ISingletonDependency
{
    private readonly RateLimiter _rateLimiter;
    
    public TelegramRateLimiter()
    {
        _rateLimiter = InitializeRateLimiter();
    }
    
    private RateLimiter InitializeRateLimiter()
    {
        return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            ReplenishmentPeriod = TimeSpan.FromSeconds(60),
            TokenLimit = 1,
            TokensPerPeriod = 1
        });
    }
    
    public Task RecordRequestAsync()
    {
        var rateLimitLease = _rateLimiter.AttemptAcquire(1);
        if (!rateLimitLease.IsAcquired)
        {
            throw new UserFriendlyException("The request exceeded the limit.");
        }
        return Task.CompletedTask;
    }
}
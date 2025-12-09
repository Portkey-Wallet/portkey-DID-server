using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using CAServer.Telegram.Options;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.Telegram;

public class TelegramRateLimiter : ITelegramRateLimiter, ISingletonDependency
{
    private readonly RateLimiter _rateLimiter;
    private readonly IOptionsMonitor<TelegramVerifierOptions> _optionsMonitor;
    
    public TelegramRateLimiter(IOptionsMonitor<TelegramVerifierOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
        _rateLimiter = InitializeRateLimiter();
    }
    
    private RateLimiter InitializeRateLimiter()
    {
        return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            ReplenishmentPeriod = TimeSpan.FromSeconds(_optionsMonitor.CurrentValue.ReplenishmentPeriodSeconds),
            TokenLimit = _optionsMonitor.CurrentValue.TokenLimit,
            TokensPerPeriod = _optionsMonitor.CurrentValue.TokensPerPeriod
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
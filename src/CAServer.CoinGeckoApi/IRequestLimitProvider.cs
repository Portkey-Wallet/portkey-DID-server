using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.CoinGeckoApi
{
    public interface IRequestLimitProvider
    {
        Task RecordRequestAsync();
    }

    public class RequestLimitProvider : IRequestLimitProvider, ISingletonDependency
    {
        private readonly IOptionsMonitor<CoinGeckoOptions> _coinGeckoOptions;
        private readonly RateLimiter _rateLimiter;

        public RequestLimitProvider(IOptionsMonitor<CoinGeckoOptions> coinGeckoOptions)
        {
            _coinGeckoOptions = coinGeckoOptions;
            _rateLimiter = InitializeRateLimiter();
        }

        private RateLimiter InitializeRateLimiter()
        {
            return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                ReplenishmentPeriod = TimeSpan.FromSeconds(_coinGeckoOptions.CurrentValue.ReplenishmentPeriod),
                TokenLimit = _coinGeckoOptions.CurrentValue.TokenLimit,
                TokensPerPeriod = _coinGeckoOptions.CurrentValue.TokensPerPeriod
            });
        }

        public Task RecordRequestAsync()
        {
            var rateLimitLease = _rateLimiter.AttemptAcquire(1);
            if (!rateLimitLease.IsAcquired)
            {
                throw new RequestExceedingLimitException("The request exceeded the limit.");
            }
            return Task.CompletedTask;
        }
    }
}
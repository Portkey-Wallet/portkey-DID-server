using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.CoinGeckoApi{

    public interface IRequestLimitProvider
    {
        Task RecordRequestAsync();
    }

    public class RequestLimitProvider : IRequestLimitProvider, ISingletonDependency
    {
        private readonly IDistributedCache<RequestTime> _requestTimeCache;

        // The CoinGecko limit 10-30 requests/minute;
        private const int MaxRequestTime = 100;

        public RequestLimitProvider(IDistributedCache<RequestTime> requestTimeCache)
        {
            _requestTimeCache = requestTimeCache;
        }

        public async Task RecordRequestAsync()
        {
            var requestTime = await _requestTimeCache.GetOrAddAsync(CoinGeckoApiConsts.RequestTimeCacheKey,
                async () => new RequestTime(), () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(1)
                });
            requestTime.Time += 1;

            if (requestTime.Time > MaxRequestTime)
            {
                throw new RequestExceedingLimitException("The request exceeded the limit.");
            }

            await _requestTimeCache.SetAsync(CoinGeckoApiConsts.RequestTimeCacheKey, requestTime);
        }
    }

    public class RequestTime
    {
        public int Time { get; set; }
    }
}
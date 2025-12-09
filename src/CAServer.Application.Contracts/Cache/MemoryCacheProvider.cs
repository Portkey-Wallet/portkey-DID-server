using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Volo.Abp.DependencyInjection;

namespace CAServer.Cache
{
    public interface ILocalMemoryCache<T>
    {
        // Do not use a dynamic cache Key to avoid this cache causing memory overflow.
        Task<T> GetOrAddAsync(string cacheKey, Func<Task<T>> factory, MemoryCacheEntryOptions options);
    }

    public class LocalMemoryCache<T> : ILocalMemoryCache<T>, ISingletonDependency
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        
        public LocalMemoryCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        // Do not use a dynamic cache Key to avoid this cache causing memory overflow.
        public async Task<T> GetOrAddAsync(string cacheKey, Func<Task<T>> factory, MemoryCacheEntryOptions options)
        {
            if (_memoryCache.TryGetValue(cacheKey, out T cachedValue))
            {
                return cachedValue;
            }

            var myLock = _locks.GetOrAdd(cacheKey, k => new SemaphoreSlim(1, 1));

            await myLock.WaitAsync();
            try
            {
                // Double-check locking pattern
                if (_memoryCache.TryGetValue(cacheKey, out cachedValue))
                {
                    return cachedValue;
                }
                cachedValue = await factory();
                _memoryCache.Set(cacheKey, cachedValue, options);
                return cachedValue;
            }
            finally
            {
                myLock.Release();
                if (_locks.TryRemove(cacheKey, out var semaphore))
                {
                    semaphore.Dispose();
                }
            }
        }
    }
}
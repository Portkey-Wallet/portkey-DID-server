using System;
using System.Threading.Tasks;
using CAServer.Commons;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;

namespace CAServer.Common;

public interface IBackgroundWorkerRegistrarProvider
{
    /// <summary>
    /// register the current node as a worker node or refresh the node expiration time.
    /// </summary>
    /// <param name="worker"></param>
    /// <param name="period">background work period, seconds</param>
    /// <param name="workerNodeExpirationTime">seconds</param>
    /// <param name="nodeName"></param>
    /// <returns></returns>
    public Task<bool> RegisterUniqueWorkerNodeAsync(string worker, int period, int workerNodeExpirationTime,
        string nodeName = null);

    public Task TryRemoveWorkerNodeAsync(string worker, string nodeName = null);
}

public class BackgroundWorkerRegistrarProvider : IBackgroundWorkerRegistrarProvider, ISingletonDependency
{
    private readonly ILogger<BackgroundWorkerRegistrarProvider> _logger;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly string _hostName;

    private const string DistributedCachePrefix = "WorkerNodeRegistrar";
    private const string DistributedLockPrefix = "WorkerNodeRegistrarLock";

    public BackgroundWorkerRegistrarProvider(ILogger<BackgroundWorkerRegistrarProvider> logger,
        IDistributedCache<string> distributedCache, IAbpDistributedLock distributedLock)
    {
        _logger = logger;
        _distributedCache = distributedCache;
        _distributedLock = distributedLock;
        _hostName = HostHelper.GetLocalHostName();
    }

    public async Task<bool> RegisterUniqueWorkerNodeAsync(string worker, int period, int workerNodeExpirationTime,
        string nodeName = null)
    {
        nodeName ??= _hostName;
        _logger.LogDebug("register unique worker node start... {0}, {1}", worker, _hostName);
        try
        {
            var distributedCacheKey = $"{DistributedCachePrefix}:{worker}";
            if (GetAndRefreshWorkerNode(distributedCacheKey, nodeName, period, out bool result))
            {
                return result;
            }

            var distributedLockKey = $"{DistributedLockPrefix}:{worker}";
            using (var lockHandle = _distributedLock.TryAcquireAsync(distributedLockKey))
            {
                if (lockHandle == null)
                {
                    _logger.LogWarning("register unique worker node result: {0}, lock failed", bool.FalseString);
                    return false;
                }

                if (GetAndRefreshWorkerNode(distributedCacheKey, nodeName, period, out result))
                {
                    return result;
                }

                await _distributedCache.SetAsync(distributedCacheKey, nodeName, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(period + period / 2 + workerNodeExpirationTime),
                    //SlidingExpiration = TimeSpan.FromSeconds(period + period / 2)
                });
                _logger.LogDebug("register unique worker node result: {0}", bool.TrueString);
                return true;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "register unique worker node error");
            return false;
        }
    }

    public async Task TryRemoveWorkerNodeAsync(string worker, string nodeName = null)
    {
        try
        {
            _logger.LogWarning("try to remove worker node start...");
            nodeName ??= _hostName;
            var distributedCacheKey = $"{DistributedCachePrefix}:{worker}";
            var currentNode = _distributedCache.Get(distributedCacheKey);
            if (!currentNode.IsNullOrWhiteSpace() && currentNode == nodeName)
            {
                await _distributedCache.RemoveAsync(distributedCacheKey);
                _logger.LogWarning("try to remove worker node end...");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "try to remove worker node error...");
        }
    }

    private bool GetAndRefreshWorkerNode(string key, string nodeName, int period, out bool result)
    {
        var currentNode = _distributedCache.Get(key);
        if (currentNode.IsNullOrWhiteSpace())
        {
            result = false;
            return false;
        }

        if (currentNode != nodeName)
        {
            _logger.LogDebug("register unique worker node result: {0}, current node is {1}", bool.FalseString,
                currentNode);
            result = false;
        }
        else
        {
            _distributedCache.Set(key, nodeName, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(period + period / 2),
            });
            _logger.LogDebug("register unique worker node result: {0}, is current node", bool.TrueString);
            result = true;
        }

        return true;
    }
}
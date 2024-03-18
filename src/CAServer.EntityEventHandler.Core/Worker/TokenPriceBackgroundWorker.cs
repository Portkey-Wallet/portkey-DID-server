using System;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Tokens.TokenPrice;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class TokenPriceBackgroundWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ILogger<TokenPriceBackgroundWorker> _logger;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly TokenPriceWorkerOption _tokenPriceWorkerOption;
    private readonly ITokenPriceService _tokenPriceService;

    private readonly string _hostName;
    private readonly string _workerNameKey;
    private readonly string _workerLockKey;

    public TokenPriceBackgroundWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IAbpDistributedLock distributedLock, ILogger<TokenPriceBackgroundWorker> logger,
        IDistributedCache<string> distributedCache, IOptionsMonitor<TokenPriceWorkerOption> tokenPriceWorkerOption,
        ITokenPriceService tokenPriceService) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _distributedLock = distributedLock;
        _distributedCache = distributedCache;
        _tokenPriceService = tokenPriceService;
        _tokenPriceWorkerOption = tokenPriceWorkerOption.CurrentValue;

        timer.Period = _tokenPriceWorkerOption.Period * 1000;
        timer.RunOnStart = true;

        _hostName = $"{HostHelper.GetLocalHostName()}_{Guid.NewGuid()}";
        _workerNameKey = $"{_tokenPriceWorkerOption.Prefix}:{_tokenPriceWorkerOption.WorkerNameKey}";
        _workerLockKey = $"{_tokenPriceWorkerOption.Prefix}:{_tokenPriceWorkerOption.WorkerLockKey}";
    }

    public override async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("stopping token price background worker start...");
        await base.StopAsync(cancellationToken);

        try
        {
            var workName = await _distributedCache.GetAsync(_workerNameKey);
            if (!workName.IsNullOrWhiteSpace() && workName != this._hostName)
            {
                return;
            }

            _logger.LogInformation("TokenPriceWorker:remove current worker... {0}", workName);
            await _distributedCache.RemoveAsync(_workerNameKey);
            _logger.LogInformation("TokenPriceWorker:remove current worker finished...");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TokenPriceWorker: stop Workder error.");
        }
        _logger.LogInformation("stoping token price background worker finished...");
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogInformation("token price background worker start... {0}", _hostName);
        try
        {
            var workName = await _distributedCache.GetAsync(_workerNameKey);
            if (!workName.IsNullOrWhiteSpace() && workName != this._hostName)
            {
                _logger.LogInformation("TokenPriceWorker: running worker: {0}", workName);
                return;
            }

            using (var lockHandle = _distributedLock.TryAcquireAsync(_workerLockKey))
            {
                if (lockHandle == null)
                {
                    _logger.LogWarning("TokenPriceWorker: lock fail.");
                    return;
                }

                _logger.LogInformation("TokenPriceWorker: update current worker... {0}", _hostName);
                await _distributedCache.SetAsync(_workerNameKey, _hostName, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(_tokenPriceWorkerOption.Period + _tokenPriceWorkerOption.Period / 2)
                });
                _logger.LogInformation("TokenPriceWorker: update token price....");

                await _tokenPriceService.RefreshCurrentPriceAsync();

                _logger.LogInformation("TokenPriceWorker: update token price finished...");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TokenPriceWorker: task error.");
        }
        _logger.LogInformation("token price background worker finished...");
    }
}
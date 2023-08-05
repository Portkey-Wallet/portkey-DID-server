using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Grain.Tests;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans.TestingHost;
using Volo.Abp.DistributedLocking;

namespace CAServer;

public abstract class CAServerApplicationTestBase : CAServerTestBase<CAServerApplicationTestModule>
{
    protected readonly TestCluster Cluster;

    protected CAServerApplicationTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetMockAbpDistributedLockAlwaysSuccess());
    }

    protected IAbpDistributedLock GetMockAbpDistributedLockAlwaysSuccess()
    {
        var mockLockProvider = new Mock<IAbpDistributedLock>();
        mockLockProvider
            .Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns<string, TimeSpan, CancellationToken>((name, timeSpan, cancellationToken) => 
                Task.FromResult<IAbpDistributedLockHandle>(new LocalAbpDistributedLockHandle(new SemaphoreSlim(0))));
        return mockLockProvider.Object;
    }

    protected IAbpDistributedLock GetMockAbpDistributedLock()
    {
        var mockLockProvider = new Mock<IAbpDistributedLock>();
        var keyRequestTimes = new Dictionary<string, DateTime>();
        mockLockProvider
            .Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns<string, TimeSpan, CancellationToken>((name, timeSpan, cancellationToken) =>
            {
                lock (keyRequestTimes)
                {
                    if (keyRequestTimes.TryGetValue(name, out var lastRequestTime))
                        if ((DateTime.Now - lastRequestTime).TotalMilliseconds <= 1000)
                            return Task.FromResult<IAbpDistributedLockHandle>(null);
                    keyRequestTimes[name] = DateTime.Now;
                    var handleMock = new Mock<IAbpDistributedLockHandle>();
                    handleMock.Setup(h => h.DisposeAsync()).Callback(() =>
                    {
                        lock (keyRequestTimes)
                            keyRequestTimes.Remove(name);
                    });

                    return Task.FromResult(handleMock.Object);
                }
            });

        return mockLockProvider.Object;
    }
    
}
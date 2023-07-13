using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Volo.Abp.DistributedLocking;

namespace CAServer;

public abstract class CAServerApplicationTestBase : CAServerTestBase<CAServerApplicationTestModule>
{
    
    protected IAbpDistributedLock GetMockAbpDistributedLock()
    {
        var mockLockProvider = new Mock<IAbpDistributedLock>();
        mockLockProvider
            .Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns<string, TimeSpan, CancellationToken>((name, timeSpan, cancellationToken) => 
                Task.FromResult<IAbpDistributedLockHandle>(new LocalAbpDistributedLockHandle(new SemaphoreSlim(0))));
        return mockLockProvider.Object;
    }

    
}
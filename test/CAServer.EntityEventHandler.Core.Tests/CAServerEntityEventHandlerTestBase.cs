using CAServer.Orleans.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.DistributedLocking;

namespace CAServer.EntityEventHandler.Tests;

public class CAServerEntityEventHandlerTestBase : CAServerOrleansTestBase<CAServerEntityEventHandlerTestModule>
{
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockAbpDistributedLock());
    }
    
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
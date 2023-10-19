using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CAServer.CAActivity.Provider;
using CAServer.Entities.Es;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Volo.Abp.DistributedLocking;
using Xunit.Abstractions;

namespace CAServer;

public abstract class CAServerApplicationTestBase : CAServerTestBase<CAServerApplicationTestModule>
{
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        //services.AddSingleton(GetMockAbpDistributedLockAlwaysSuccess());
        services.AddSingleton(GetMockInMemoryHarness());
    }

    protected IActivityProvider MockActivityProviderCaHolder(string guidVal = null)
    {
        return GetMockActivityProvider(new CAHolderIndex
        {
            UserId = guidVal.IsNullOrEmpty() ? Guid.NewGuid() : Guid.Parse(guidVal)
        });
    }
    
    protected IBus GetMockInMemoryHarness(params IConsumer[] consumers)
    {
        var busMock = new Mock<IBus>();

        busMock.Setup(bus => bus.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((message, token) =>
            {
                foreach (var consumer in consumers)
                {
                    var consumeMethod = consumer.GetType().GetMethod("Consume");
                    if (consumeMethod == null) continue;
                
                    var consumeContextType = typeof(ConsumeContext<>).MakeGenericType(message.GetType());
                
                    dynamic contextMock = Activator.CreateInstance(typeof(Mock<>).MakeGenericType(consumeContextType));
                    contextMock.SetupGet("Message").Returns(message);
                
                    consumeMethod.Invoke(consumer, new[] { ((Mock)contextMock).Object });
                }
            })
            .Returns(Task.CompletedTask);

        return busMock.Object;
    }


    
    protected IActivityProvider GetMockActivityProvider(CAHolderIndex result = null)
    {
        var mockActivityProvider = new Mock<IActivityProvider>();
        mockActivityProvider
            .Setup(x => x.GetCaHolder(It.IsAny<string>()))
            .Returns<string>((_) => Task.FromResult(result));
        
        mockActivityProvider.Setup(m => m.GetTokenDecimalsAsync(It.IsAny<string>())).ReturnsAsync(new IndexerSymbols
        {
            TokenInfo = new List<SymbolInfo>
            {
                new()
                {
                    Decimals = 8
                }
            }
        });
        return mockActivityProvider.Object;
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
    
    public static IHttpClientFactory MockHttpFactory(ITestOutputHelper testOutputHelper,
        params Action<Mock<HttpMessageHandler>, ITestOutputHelper>[] mockActions)
    {
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        foreach (var mockFunc in mockActions)
            mockFunc.Invoke(mockHandler, testOutputHelper);

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(_ => _.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://test.com/") });

        return httpClientFactoryMock.Object;
    }

    public static Action<Mock<HttpMessageHandler>, ITestOutputHelper> PathMatcher(HttpMethod method, string path,
        string respData)
    {
        
        return (mockHandler, testOutputHelper) =>
        {
            DateTimeOffset offset = DateTime.UtcNow.AddDays(7);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(respData, Encoding.UTF8, "application/json")
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == method && req.RequestUri.ToString().Contains(path)),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() =>
                {
                    testOutputHelper?.WriteLine($"Mock Http {method} to {path}, resp={response}");
                    return Task.FromResult(response);
                });
        };
    }

    public static Action<Mock<HttpMessageHandler>, ITestOutputHelper> PathMatcher(HttpMethod method, string path, object response)
    {
        return PathMatcher(method, path, JsonConvert.SerializeObject(response));
    }
    
}
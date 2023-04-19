using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Hubs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace CAServer.Hub;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class HubServiceTest : CAServerApplicationTestBase
{
    protected IHubService _hubService;
    private readonly ConcurrentDictionary<string, string> _connectId2ClientId = new();

    public HubServiceTest()
    {
        _hubService = GetRequiredService<IHubService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetHubProvider());
        // services.AddSingleton(GetConnectionProvider());
        services.AddSingleton(GetHubCacheProvider());
    }

    [Fact]
    public async void PingTest()
    {
        await _hubService.Ping(new HubRequestContext { RequestId = "1", ClientId = "3433" }, "content");
    }

    [Fact]
    public async void GetResponseTest()
    {
        var res = await _hubService.GetResponse(new HubRequestContext { RequestId = "456", ClientId = "12121" });
        Assert.True(res == null);
        res = await _hubService.GetResponse(new HubRequestContext { RequestId = "123" });
        res.RequestId.ShouldBe("123");
        res.Body.ShouldBe("456");
    }

    [Fact]
    public async void RegisterClientTest()
    {
        var clientId = "1";
        var connectionId = "XXX";
        var res = _hubService.UnRegisterClient(connectionId);
        Assert.True(res == null);
        await _hubService.RegisterClient(clientId, connectionId);
        res = _hubService.UnRegisterClient(connectionId);
        res.ShouldBe(clientId);
    }

    [Fact]
    public async void SendAllUnreadResTest()
    {
        var clientId = "123";
        var clientId2 = "456";
        await _hubService.SendAllUnreadRes(clientId);
        await _hubService.SendAllUnreadRes(clientId2);
    }

    [Fact]
    public async void Ack()
    {
        await _hubService.Ack("123", "456");
    }

    private IHubProvider GetHubProvider()
    {
        var provider = new Mock<IHubProvider>();
        // provider.Setup(m => m.ResponseAsync(It.IsAny<HubResponseBase<object>>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        // provider.Setup(m => m.ResponseAsync(It.IsAny<HubResponseBase<object>>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        provider.Setup(m => m.ResponseAsync(It.IsAny<HubResponseBase<string>>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        return provider.Object;
    }

    private IConnectionProvider GetConnectionProvider()
    {
        var provider = new Mock<IConnectionProvider>();

        provider.Setup(p => p.Add(
            It.IsAny<string>(), It.IsAny<string>())).Callback((string clientId, string connectionId) => { _connectId2ClientId[connectionId] = clientId; }).Verifiable();
        provider.Setup(p => p.Remove(It.IsAny<string>())).Returns((string connectionId) =>
        {
            if (!_connectId2ClientId.ContainsKey(connectionId))
            {
                return null;
            }

            var clientId = _connectId2ClientId[connectionId];
            _connectId2ClientId.TryRemove(connectionId, out var _);
            return clientId;
        });
        return provider.Object;
    }

    private IHubCacheProvider GetHubCacheProvider()
    {
        HubResponseCacheEntity<object> nullRes = null;
        var provider = new Mock<IHubCacheProvider>();
        provider.Setup(m => m.GetRequestById(It.IsAny<string>())).Returns((string requestId) =>
            requestId == "123"
                ? Task.FromResult(
                    new HubResponseCacheEntity<object>
                    {
                        Response = new HubResponse<object>() { RequestId = "123", Body = "456" }
                    })
                : Task.FromResult(nullRes));

        provider.Setup(m => m.GetResponseByClientId(It.IsAny<string>())).Returns(
            (string clientId) =>
                clientId == "123"
                    ? Task.FromResult(new List<HubResponseCacheEntity<object>>
                    {
                        new()
                        {
                            Response = new HubResponse<object>() { RequestId = "123", Body = "456" }
                        }
                    })
                    : Task.FromResult(new List<HubResponseCacheEntity<object>> { }));
        return provider.Object;
    }
}
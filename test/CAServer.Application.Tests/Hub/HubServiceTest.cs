using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Hubs;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute.ReturnsExtensions;
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
        base.AfterAddApplication(services);
        services.AddSingleton(GetHubProvider());
        // services.AddSingleton(GetConnectionProvider());
        services.AddSingleton(GetHubCacheProvider());
        services.AddSingleton(GetThirdPartOrderProvider());
        services.AddSingleton(GetMockThirdPartOptions());
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

    [Fact]
    public async void RequestAchTxAddressTest()
    {
        //  test after regist
        await _hubService.RequestAchTxAddressAsync("123", "00000000-0000-0000-0000-000000000001");
        
        // test after register client
        await _hubService.RegisterClient("123", "conn-123123");
        await _hubService.RequestAchTxAddressAsync("123", "00000000-0000-0000-0000-000000000001");
        await _hubService.RequestAchTxAddressAsync("123", "00000000-0000-0000-0000-000000000002");
        await _hubService.RequestAchTxAddressAsync("123", "00000000-0000-0000-0000-000000000003");
        
    }

    private IHubProvider GetHubProvider()
    {
        var provider = new Mock<IHubProvider>();
        // provider.Setup(m => m.ResponseAsync(It.IsAny<HubResponseBase<object>>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        // provider.Setup(m => m.ResponseAsync(It.IsAny<HubResponseBase<object>>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        provider.Setup(m => m.ResponseAsync(It.IsAny<HubResponseBase<string>>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        return provider.Object;
    }


    private IThirdPartOrderProvider GetThirdPartOrderProvider()
    {
        var mockProvider = new Mock<IThirdPartOrderProvider>();

        var noAddressData = new OrderDto
        {
            Id = new Guid("00000000-0000-0000-0000-000000000001"),
        };

        var withAddressData = new OrderDto
        {
            Id = new Guid("00000000-0000-0000-0000-000000000002"),
            Address = "123456"
        };
        
        mockProvider.Setup(provider => provider.GetThirdPartOrderAsync(It.Is<string>(id => id == "00000000-0000-0000-0000-000000000001"))).ReturnsAsync(noAddressData);
        mockProvider.Setup(provider => provider.GetThirdPartOrderAsync(It.Is<string>(id => id == "00000000-0000-0000-0000-000000000002"))).ReturnsAsync(withAddressData);
        mockProvider.Setup(provider => provider.GetThirdPartOrderAsync(It.Is<string>(id => id == "00000000-0000-0000-0000-000000000003"))).ReturnsAsync((OrderDto)null);
        

        return mockProvider.Object;
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
    
    
    private IOptionsMonitor<ThirdPartOptions> GetMockThirdPartOptions()
    {
        var thirdPartOptions = new ThirdPartOptions()
        {
            Alchemy = new AlchemyOptions()
            {
                AppId = "12344fdsfdsfdsfsdfdsfsdfsdfdsfsdfa",
                BaseUrl = "http://localhost:9200/book/_search",
                SkipCheckSign = true
            },
            Timer =  new ThirdPartTimerOptions()
            {
                TimeoutMillis = 100,
                DelaySeconds = 1,
            }
        };
        
        var mockOption = new Mock<IOptionsMonitor<ThirdPartOptions>>();
        mockOption.Setup(o => o.CurrentValue).Returns(thirdPartOptions);
        return mockOption.Object;
    }
}
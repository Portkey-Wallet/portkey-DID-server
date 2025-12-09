using System;
using CAServer.Cache;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CAServer.Hub;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class HubTests : CAServerApplicationTestBase
{
    protected IHubCacheProvider _hubCacheProvider;


    public HubTests()
    {
        _hubCacheProvider = new HubCacheProvider(GetMockCacheProvider(),
            GetHubCacheOptions(), NullLogger<HubCacheProvider>.Instance);
    }


    [Fact]
    public async void SetResponseAsync()
    {
        var clientId = Guid.NewGuid().ToString();
        var cacheVal = new HubResponseCacheEntity<string>("123", "456", "789", typeof(string));
        await _hubCacheProvider.SetResponseAsync(cacheVal, clientId);
        var rightVal = await _hubCacheProvider.GetRequestById(cacheVal.Response.RequestId);
        rightVal.Type.ShouldBe(typeof(string));
        rightVal.Method.ShouldBe(cacheVal.Method);
        rightVal.Response.RequestId.ShouldBe(cacheVal.Response.RequestId);
        rightVal.Response.Body.ShouldBe(cacheVal.Response.Body);


        var listVal = await _hubCacheProvider.GetResponseByClientId(clientId);
        listVal.Count.ShouldBe(1);
        listVal[0].Response.Body.ShouldBe(cacheVal.Response.Body);

        var wrongVal = await _hubCacheProvider.GetRequestById("$$$@");
        Assert.True(wrongVal == null);
    }

    private ICacheProvider GetMockCacheProvider()
    {
        return new MockCacheProvider();
    }

    private IOptions<HubCacheOptions> GetHubCacheOptions()
    {
        return new OptionsWrapper<HubCacheOptions>(
            new HubCacheOptions
            {
                MethodResponseTtl = new() { },
                DefaultResponseTtl = 1111
            });
    }
}
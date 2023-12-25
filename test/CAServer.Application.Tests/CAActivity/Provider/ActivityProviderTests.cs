using System;
using System.Collections.Generic;
using CAServer.Common;
using CAServer.UserAssets;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CAServer.CAActivity.Provider;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class ActivityProviderTests : CAServerApplicationTestBase
{
    private IActivityProvider _activityProvider;
    
    public ActivityProviderTests()
    {
        _activityProvider = GetRequiredService<IActivityProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockIGraphQLHelper());
    }
    [Fact]
    public async void GetActivitiesAsyncTest()
    {
        var result = await _activityProvider.GetActivitiesAsync(new List<CAAddressInfo>
        {
            new() {CaAddress = "123", ChainId = "TDVW"}
        }, "AElf", "AElf", new List<string>(), 0, 10);
    }
    
    [Fact]
    public async void GetActivityAsyncTest()
    {
        var result = await _activityProvider.GetActivityAsync("123", "123", new List<CAAddressInfo>());
    }
    
    [Fact]
    public async void GetCaHolderNickNameTest()
    {
        var result = await _activityProvider.GetCaHolderNickName(Guid.NewGuid());
    }
    
    [Fact]
    public async void GetTokenDecimalsAsyncTest()
    {
        var result = await _activityProvider.GetTokenDecimalsAsync("123");
    }
    
    
    private IGraphQLHelper GetMockIGraphQLHelper()
    {
        var mockHelper = new Mock<IGraphQLHelper>();
        return mockHelper.Object;
    }
}
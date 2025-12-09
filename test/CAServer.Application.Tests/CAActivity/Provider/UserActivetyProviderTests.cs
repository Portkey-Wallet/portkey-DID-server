using System.Collections.Generic;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.UserAssets;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CAServer.CAActivity.Provider;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class UserActivetyProviderTests : CAServerApplicationTestBase
{
    private readonly IActivityProvider _uActivityProvider;

    public UserActivetyProviderTests()
    {
        _uActivityProvider = GetRequiredService<IActivityProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockIGraphQlHelper());
    }
    // IActivityProvider

    [Fact]
    public async void GraphQlTest()
    {
        var activitiesList = new List<CAAddressInfo>();
        var activitiesListstr = new List<string>();

        await _uActivityProvider.GetActivitiesAsync(
            activitiesList, "", "", activitiesListstr,
            1, 1);

        await _uActivityProvider.GetActivityAsync("", "", new List<CAAddressInfo>());
    }


    private IGraphQLHelper GetMockIGraphQlHelper()
    {
        var mockHelper = new Mock<IGraphQLHelper>();
        return mockHelper.Object;
    }
}
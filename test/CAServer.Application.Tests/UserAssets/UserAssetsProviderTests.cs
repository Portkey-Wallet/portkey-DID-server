using System;
using System.Collections.Generic;
using CAServer.Common;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CAServer.UserAssets;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class UserAssetsProviderTests : CAServerApplicationTestBase
{
    private IUserAssetsProvider _userAssetsProvider;

    public UserAssetsProviderTests()
    {
        _userAssetsProvider = GetRequiredService<IUserAssetsProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockIGraphQLHelper());
    }


    [Fact]
    public async void GraphQlTest()
    {
        await _userAssetsProvider.GetUserChainIdsAsync(new List<string>
        {
            "123"
        });
        await _userAssetsProvider.GetUserTokenInfoAsync(new List<CAAddressInfo>
        {
            new() { CaAddress = "123", ChainId = "TDVW" }
        }, "AElf", 0, 10);
        await _userAssetsProvider.GetUserNftCollectionInfoAsync(new List<CAAddressInfo>
            {
                new() { CaAddress = "123", ChainId = "TDVW" }
            }, 0, 10
        );
        await _userAssetsProvider.GetUserNftInfoAsync(new List<CAAddressInfo>
        {
            new() { CaAddress = "123", ChainId = "TDVW" }
        }, "AElf", 0, 10);
        await _userAssetsProvider.GetRecentTransactionUsersAsync(new List<CAAddressInfo>
        {
            new() { CaAddress = "123", ChainId = "TDVW" }
        }, 0, 10);
        await _userAssetsProvider.SearchUserAssetsAsync(new List<CAAddressInfo>
        {
            new() { CaAddress = "123", ChainId = "TDVW" }
        }, "666", 0, 10);
    }

    [Fact]
    public async void TokenIndexRepositoryTest()
    {
        var userId = Guid.NewGuid();
        await _userAssetsProvider.GetUserDefaultTokenSymbolAsync(userId);
        await _userAssetsProvider.GetUserIsDisplayTokenSymbolAsync(userId);
        await _userAssetsProvider.GetUserNotDisplayTokenAsync(userId);
    }

    private IGraphQLHelper GetMockIGraphQLHelper()
    {
        var mockHelper = new Mock<IGraphQLHelper>();
        return mockHelper.Object;
    }
}
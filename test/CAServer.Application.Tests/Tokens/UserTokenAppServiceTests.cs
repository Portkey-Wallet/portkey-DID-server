using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Grain.Tests;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.Tokens.UserTokens;
using CAServer.Security;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.Tokens;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class UserTokenAppServiceTests : CAServerApplicationTestBase
{
    private readonly IUserTokenAppService _userTokenAppService;
    protected readonly TestCluster Cluster;

    protected ICurrentUser _currentUser;

    public UserTokenAppServiceTests()
    {
        _userTokenAppService = GetRequiredService<IUserTokenAppService>();
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
        services.AddSingleton(_currentUser);
        
        var graphQlHelper = Substitute.For<IGraphQLHelper>();
        var graphQlClient = Substitute.For<IGraphQLClient>();
        services.AddSingleton(graphQlClient);
        services.AddSingleton(graphQlHelper);

        services.AddSingleton(GetMockSymbolCache());
        services.AddSingleton(GetMockITokenProvider());
    }

    private void Login(Guid userId)
    {
        _currentUser.Id.Returns(userId);
        _currentUser.IsAuthenticated.Returns(true);
    }

    [Fact]
    public async Task ChangeTokenDisplayAsyncTest()
    {
        var userId = _currentUser.GetId();
        var display = false;

        var grain = Cluster.Client.GetGrain<IUserTokenGrain>(userId);
        var token = new UserTokenGrainDto
        {
            UserId = userId,
            IsDefault = true,
            IsDisplay = true,
            Token = new Tokens.Dtos.Token
            {
                Id = Guid.NewGuid(),
                Symbol = "CPU",
                ChainId = "AELF",
                Address = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
                Decimals = 8
            }
        };
        await grain.AddUserTokenAsync(userId, token);

        var result = _userTokenAppService.ChangeTokenDisplayAsync(display, userId.ToString());
        var data = result.Result;
        data.IsDisplay.ShouldBe(display);
    }

    [Fact]
    public async Task Change_None_Resource_Token_Display_Async_Test()
    {
        try
        {
            var display = true;
            var result = await _userTokenAppService.ChangeTokenDisplayAsync(display, "AELF-VOTE");
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("Token not found.");
        }

    }
    
    [Fact]
    public async Task AddUserTokenAsyncTest()
    {
        var userId = _currentUser.GetId();
        var isDisplay = false;
        var isDefault = false;
        var token = new AddUserTokenInput
        {
            IsDefault = isDefault,
            IsDisplay = isDisplay,
            Token = new Tokens.Dtos.Token
            {
                Id = Guid.NewGuid(),
                Symbol = "AELF",
                ChainId = "AELF",
                Address = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
                Decimals = 8
            }
        };
        var result = _userTokenAppService.AddUserTokenAsync(userId, token);
        var data = result.Result;
        data.IsDisplay.ShouldBe(isDisplay);
        data.IsDefault.ShouldBe(isDefault);
    }
}
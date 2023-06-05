using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Grain.Tests;
using CAServer.Grains;
using CAServer.Grains.Grain.Tokens.TokenPrice;
using CAServer.Grains.Grain.Tokens.UserTokens;
using CAServer.Security;
using CAServer.Tokens.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.Tokens;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class TokenAppServiceHistoryTest : CAServerApplicationTestBase
{
    private readonly ITokenAppService _tokenAppService;
    public const string Symbol = "AELF";
    protected readonly TestCluster Cluster;
    protected ICurrentUser _currentUser;

    public TokenAppServiceHistoryTest()
    {
        _tokenAppService = GetRequiredService<ITokenAppService>();
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
        services.AddSingleton(GetMockHttpClientFactory());
        services.AddSingleton(_currentUser);
        services.AddSingleton(GetMockContractAddressOptions());
        services.AddSingleton(GetMockTokenPriceProvider());
        services.AddSingleton(GetMockTokenPriceExpirationTimeOptions());
        services.AddSingleton(GetMockTokenPriceSnapshotClusterClient());
        services.AddSingleton(GetMockTokenPriceSnapshotGrain());

    }
    
    [Fact]
    public async Task GetTokenHistoryPriceDataAsyncTest()
    {
        var input = new GetTokenHistoryPriceInput()
        {
            DateTime = DateTime.Now.AddDays(-1)
        };
        var result = await _tokenAppService.GetTokenHistoryPriceDataAsync(new List<GetTokenHistoryPriceInput>() { input });
        result.Items.Count.ShouldBe(1);
 
        input = new GetTokenHistoryPriceInput()
        {
            DateTime = DateTime.Now.AddDays(-1),
            Symbol = Symbol
        };
        result = await _tokenAppService.GetTokenHistoryPriceDataAsync(new List<GetTokenHistoryPriceInput>() { input });
        result.Items.Count.ShouldBe(1);
        result.Items.First().Symbol.ShouldBe(Symbol);
    }
    
}
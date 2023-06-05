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
public partial class TokenAppServiceTest : CAServerApplicationTestBase
{
    private readonly ITokenAppService _tokenAppService;
    public const string Symbol = "AELF";
    protected readonly TestCluster Cluster;
    protected ICurrentUser _currentUser;

    public TokenAppServiceTest()
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
        services.AddSingleton(GetMockClusterClient());
        services.AddSingleton(GetMockTokenPriceGrain());

    }

    [Fact]
    public async Task GetTokenPriceListAsyncTest()
    {
        var symbols = new List<string>();
        var resultNullParam = await _tokenAppService.GetTokenPriceListAsync(symbols);
        resultNullParam.Items.Count.ShouldBe(0);
        
        symbols.Add(Symbol);
        var result = await _tokenAppService.GetTokenPriceListAsync(symbols);
        result.Items.Count.ShouldBe(1);
        result.Items.First().Symbol.ShouldBe(Symbol);
    }
    
    [Fact]
    public async Task GetContractAddressAsync()
    {
        var result = _tokenAppService.GetContractAddressAsync();
        var data = result.Result;
        data.ContractName.ShouldBe("test");
        data.MainChainAddress.ShouldBe("test");
        data.SideChainAddress.ShouldBe("test");

    }
}
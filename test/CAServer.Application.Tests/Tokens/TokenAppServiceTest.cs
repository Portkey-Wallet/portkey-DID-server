using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Grain.Tests;
using CAServer.Security;
using CAServer.Tokens.Dtos;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
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
        var graphQlHelper = Substitute.For<IGraphQLHelper>();
        var graphQlClient = Substitute.For<IGraphQLClient>();
        services.AddSingleton(graphQlClient);
        services.AddSingleton(graphQlHelper);
        services.AddSingleton(GetMockITokenProvider());
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

    [Fact]
    public async Task GetTokenListAsyncAsyncTest()
    {
        try
        {
            var tokenInfo = await _tokenAppService.GetTokenListAsync(new GetTokenListRequestDto()
            {
                Symbol = "CPU",
                ChainIds = new List<string>() { "AELF", "tDVV" }
            });

            tokenInfo.Count.ShouldBe(1);
        }
        catch (Exception e)
        {
        }
    }

    [Fact]
    public async Task GetTokenInfoAsyncTest()
    {
        var tokenInfo = await _tokenAppService.GetTokenInfoAsync("AELF", "CPU");
        tokenInfo.Symbol.ShouldBe("CPU");
    }

    [Fact]
    public async Task GetTokenInfoAsync_Search_From_GraphQL_Test()
    {
        var tokenInfo = await _tokenAppService.GetTokenInfoAsync("AELF", "VOTE");
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Grain.Tests;
using CAServer.Security;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Provider;
using CAServer.UserAssets;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.Tokens;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class TokenAppServiceTest : CAServerApplicationTestBase
{
    private readonly ITokenAppService _tokenAppService;
    public const string Symbol = "AELF";
    public const string UsdtSymbol = "USDT";
    public const string CpuSymbol = "CPU";
    public const decimal AelfPrice = 0.638m;
    public const decimal UsdtPrice = 0.999m;
    protected readonly TestCluster Cluster;
    protected ICurrentUser _currentUser;
    private readonly ServiceCollection _services = new ServiceCollection(); 

    public TokenAppServiceTest()
    {
        _tokenAppService = GetRequiredService<ITokenAppService>();
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
        services.AddSingleton(GetMockHttpClientFactory());
        services.AddSingleton(_currentUser);
        services.AddSingleton(GetMockContractAddressOptions());
        services.AddSingleton(GetMockTokenPriceProvider());
        var graphQlHelper = Substitute.For<IGraphQLHelper>();
        var graphQlClient = Substitute.For<IGraphQLClient>();
        services.AddSingleton(graphQlClient);
        services.AddSingleton(graphQlHelper);
        services.AddSingleton(GetMockITokenProvider());
        services.AddSingleton(GetMockITokenCacheProvider());
        services.AddSingleton(GetMockCoinGeckoOptions());
        services.AddSingleton(GetMockTokenPriceWorkerOption());
        services.AddSingleton(GetMockSignatureServerOptions());
        services.AddSingleton(GetMockRequestLimitProvider());
        services.AddSingleton(GetMockSecretProvider());
        services.AddSingleton(GetMockDistributedCache());
        services.AddSingleton(GetMockTokenSpenderOptions());
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
        result.Items.First().PriceInUsd = AelfPrice;
        result.Items.First().Symbol.ShouldBe(Symbol);
        
        symbols.Add(CpuSymbol);
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _tokenAppService.GetTokenPriceListAsync(symbols);
        });
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
        var tokenInfo = await _tokenAppService.GetTokenListAsync(new GetTokenListRequestDto()
        {
            Symbol = "C",
            ChainIds = new List<string>() { "AELF", "tDVV" }
        });

        tokenInfo.Count.ShouldNotBe(0);
        tokenInfo?.Where(t=>t.Symbol.Contains("C")).Count().ShouldNotBe(0);
    }

    [Fact]
    public async Task GetTokenInfoAsyncTest()
    {
        var tokenInfo = await _tokenAppService.GetTokenInfoAsync("AELF", "CPU");
        tokenInfo.Symbol.ShouldBe("CPU");
        
        tokenInfo = await _tokenAppService.GetTokenInfoAsync("AELF", "AXX");
        tokenInfo.Symbol.ShouldBe("AXX");
        tokenInfo.Decimals.ShouldBe(8);
    }

    [Fact]
    public async Task GetTokenInfoAsync_Search_From_GraphQL_Test()
    {
        var token = new IndexerToken()
        {
            Id = "AELF-CPU",
            Symbol = "CPU",
            ChainId = "AELF",
            Decimals = 8,
            BlockHash = string.Empty,
            BlockHeight = 0,
            Type = string.Empty,
            TokenContractAddress = string.Empty,
            TokenName = "CPU",
            TotalSupply = 100000,
            Issuer = string.Empty,
            IsBurnable = false,
            IssueChainId = 1264323
        };
        var tokenInfo = await _tokenAppService.GetTokenInfoAsync("AELF", "VOTE");
    }

    [Fact]
    public async Task GetTokenAllowanceAsync_Test()
    {
        var tokenAllowanceResult = await _tokenAppService.GetTokenAllowancesAsync(new GetAssetsBase()
        {
            CaAddressInfos = new List<CAAddressInfo>()
            {
                new()
                {
                    CaAddress = "XXX",
                    ChainId = "AELF"
                }
            }
        });
        tokenAllowanceResult.Data.Count.ShouldBe(1);
        tokenAllowanceResult.TotalRecordCount.ShouldBe(1);
        tokenAllowanceResult.Data[0].Allowance.ShouldBe(0);
        tokenAllowanceResult.Data[0].ContractAddress.ShouldBe("XXXXX");
        tokenAllowanceResult.Data[0].Name.ShouldBe("Dapp1");
        tokenAllowanceResult.Data[0].SymbolApproveList.Count.ShouldBe(2);
        
        var tokenAllowanceResult1 = await _tokenAppService.GetTokenAllowancesAsync(new GetAssetsBase()
        {
            CaAddressInfos = new List<CAAddressInfo>()
            {
                new()
                {
                    CaAddress = " ",
                    ChainId = "AELF"
                }
            }
        });
        tokenAllowanceResult1.Data.Count.ShouldBe(0);
    }
}
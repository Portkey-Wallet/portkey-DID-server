using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using CAServer.AppleAuth.Provider;
using CAServer.Bookmark;
using CAServer.Common;
using CAServer.EntityEventHandler.Core;
using CAServer.Grain.Tests;
using CAServer.Hub;
using CAServer.IpInfo;
using CAServer.Options;
using CAServer.RedPackage;
using CAServer.Search;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSubstitute.Extensions;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;

namespace CAServer;

[DependsOn(
    typeof(CAServerApplicationModule),
    typeof(AbpEventBusModule),
    typeof(CAServerGrainTestModule),
    typeof(CAServerDomainTestModule)
)]
public class CAServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // load config from [appsettings.Development.json]
        var environmentName = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        // context.Services.AddSingleton(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.AddSingleton<ISearchAppService, SearchAppService>();
        context.Services.AddSingleton<IConnectionProvider, ConnectionProvider>();
        context.Services.AddSingleton<BookmarkAppService>();
        context.Services.AddSingleton<BookmarkHandler>();
        
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);

        ConfigureGraphQl(context);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAServerApplicationModule>(); });
        Configure<SwitchOptions>(options => options.Ramp = true);
        var tokenList = new List<UserTokenItem>();
        var token1 = new UserTokenItem
        {
            IsDefault = true,
            IsDisplay = true,
            SortWeight = 1,
            Token = new Token
            {
                ChainId = "AELF",
                Address = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
                Decimals = 8,
                Symbol = "ELF"
            }
        };
        var token2 = new UserTokenItem
        {
            IsDefault = false,
            IsDisplay = false,
            SortWeight = 1,
            Token = new Token
            {
                ChainId = "TDVV",
                Address = "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx",
                Decimals = 8,
                Symbol = "ELF"
            }
        };
        tokenList.Add(token1);
        tokenList.Add(token2);
        context.Services.Configure<RedPackageOptions>(o =>
        {
            o.MaxCount = 1000;
            o.TokenInfo = new List<RedPackageTokenInfo>()
            {
                new RedPackageTokenInfo()
                {
                    ChainId = "AELF",
                    Decimal = 8,
                    MinAmount = "1",
                    Symbol = "ELF"
                }
            };
        });
        context.Services.Configure<TokenListOptions>(o => { o.UserToken = tokenList; });
        context.Services.Configure<IpServiceSettingOptions>(o =>
        {
            o.BaseUrl = "http://127.0.0.1/";
            o.AccessKey = "AccessKey";
            o.Language = "en";
            o.ExpirationDays = 1;
        });
        context.Services.Configure<ThirdPartOptions>(configuration.GetSection("ThirdPart"));

        context.Services.Configure<ActivityTypeOptions>(o =>
        {
            o.TypeMap = new Dictionary<string, string>() { { "TEST", "TEST" } };
            o.TransferTypes = new List<string>() { "TEST", "TransferTypes" };
            o.ContractTypes = new List<string>() { "TEST", "ContractTypes" };
            o.ShowPriceTypes = new List<string>() { "TEST" };
            o.NoShowTypes = new List<string>() { "no show" };
            o.Zero = "0";
        });
        context.Services.Configure<CAServer.Grains.Grain.ApplicationHandler.ChainOptions>(option =>
        {
            option.ChainInfos = new Dictionary<string, CAServer.Grains.Grain.ApplicationHandler.ChainInfo>
                { { "TEST", new CAServer.Grains.Grain.ApplicationHandler.ChainInfo() } };
        });

        context.Services.Configure<CAServer.Options.ChainOptions>(option =>
        {
            option.ChainInfos = new Dictionary<string, CAServer.Options.ChainInfo>
            {
                {
                    "TEST", new CAServer.Options.ChainInfo()
                    {
                        BaseUrl = "http://127.0.0.1:6889",
                        ChainId = "TEST",
                        PrivateKey = "28d2520e2c480ef6f42c2803dcf4348807491237fd294c0f0a3d7c8f9ab8fb91"
                    }
                }
            };
        });

        context.Services.Configure<ClaimTokenInfoOptions>(option =>
        {
            option.ChainId = "TEST";
            option.PublicKey = "28d2520e2c480ef6f42c2803dcf4348807491237fd294c0f0a3d7c8f9ab8fb91";
        });
        context.Services.Configure<DefaultIpInfoOptions>(options =>
        {
            options.Country = "Singapore";
            options.Code = "SG";
            options.Iso = "65";
        });
        context.Services.Configure<SecurityOptions>(options => { options.DefaultTokenTransferLimit = 100000; });

        context.Services.Configure<AppleCacheOptions>(options =>
        {
            options.Configuration = "127.0.0.1:6379";
            options.Db = 2;
        });

        context.Services.Configure<AwsThumbnailOptions>(options =>
        {
            options.ImBaseUrl = "https:127.0.0.1";
            options.PortKeyBaseUrl = "https:127.0.0.1";
        });
        context.Services.Configure<TokenInfoOptions>(option =>
        {
            option.TokenInfos = new Dictionary<string, TokenInfo>
            {
                {"ELF", new TokenInfo()
                {
                    ImageUrl = "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/aelf/Coin_ELF.png"
                }} 
            };
        });
        base.ConfigureServices(context);
    }

    private void ConfigureGraphQl(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton(new GraphQLHttpClient(
            "http://127.0.0.1:8083/AElfIndexer_DApp/PortKeyIndexerCASchema/graphql",
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
    }
}
using System.Collections.Generic;
using CAServer.BackGround;
using CAServer.BackGround.EventHandler;
using CAServer.BackGround.Provider;
using CAServer.Bookmark;
using CAServer.Common;
using CAServer.ContractEventHandler.Core;
using CAServer.EntityEventHandler.Core;
using CAServer.EntityEventHandler.Core.ThirdPart;
using CAServer.Grain.Tests;
using CAServer.Hub;
using CAServer.IpInfo;
using CAServer.Options;
using CAServer.Search;
using CAServer.ThirdPart;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using ChainOptions = CAServer.Grains.Grain.ApplicationHandler.ChainOptions;

namespace CAServer;

[DependsOn(
    typeof(CAServerApplicationModule),
    typeof(CAServerApplicationContractsModule),
    typeof(CAServerContractEventHandlerCoreModule),
    typeof(AbpEventBusModule),
    typeof(CAServerGrainTestModule),
    typeof(CAServerDomainTestModule)
)]
public class CAServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // context.Services.AddSingleton(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.AddSingleton<ISearchAppService, SearchAppService>();
        context.Services.AddSingleton<IConnectionProvider, ConnectionProvider>();
        context.Services.AddSingleton<BookmarkAppService>();
        context.Services.AddSingleton<BookmarkHandler>();
        
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);

        ConfigureGraphQl(context);
        
        context.Services.AddMemoryCache();

        context.Services.AddSingleton<INftCheckoutService, NftCheckoutService>();
        
        context.Services.AddSingleton<INftOrderSettlementTransferWorker, NftOrderSettlementTransferWorker>();
        context.Services.AddSingleton<INftOrderThirdPartOrderStatusWorker, NftOrderThirdPartOrderStatusWorker>();
        context.Services.AddSingleton<INftOrderThirdPartNftResultNotifyWorker, NftOrderThirdPartNftResultNotifyWorker>();
        context.Services.AddSingleton<INftOrderMerchantCallbackWorker, NftOrderMerchantCallbackWorker>();
        
        context.Services.AddSingleton<NftOrderMerchantCallbackHandler>();
        context.Services.AddSingleton<NftOrderUpdateHandler>();
        context.Services.AddSingleton<NftOrderReleaseResultHandler>();
        context.Services.AddSingleton<NftOrderPaySuccessHandler>();
        context.Services.AddSingleton<NftOrderTransferHandler>();
        context.Services.AddSingleton<ThirdPartHandler>();
        
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAServerApplicationModule>(); });
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CABackGroundModule>(); });
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAServerContractEventHandlerCoreModule>(); });
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
        context.Services.Configure<TokenListOptions>(o => { o.UserToken = tokenList; });
        context.Services.Configure<IpServiceSettingOptions>(o =>
        {
            o.BaseUrl = "http://127.0.0.1/";
            o.AccessKey = "AccessKey";
            o.Language = "en";
            o.ExpirationDays = 1;
        });
        context.Services.Configure<ChainOptions>(option =>
        {
            option.ChainInfos = new Dictionary<string, Grains.Grain.ApplicationHandler.ChainInfo>
                { { "TEST", new Grains.Grain.ApplicationHandler.ChainInfo() } };
        });

        context.Services.Configure<Options.ChainOptions>(option =>
        {
            option.ChainInfos = new Dictionary<string, Options.ChainInfo>
            {
                {
                    "TEST", new Options.ChainInfo()
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
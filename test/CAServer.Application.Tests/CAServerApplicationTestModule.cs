using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
using CAServer.RedPackage;
using CAServer.Search;
using CAServer.ThirdPart;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.AutoMapper;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using ChainOptions = CAServer.Grains.Grain.ApplicationHandler.ChainOptions;

namespace CAServer;

[DependsOn(
    typeof(CAServerApplicationModule),
    typeof(CAServerApplicationContractsModule),
    // typeof(CAServerContractEventHandlerCoreModule),
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
        
        // context.Services.AddSingleton<NftOrderSettlementTransferWorker, NftOrderSettlementTransferWorker>();
        // context.Services.AddSingleton<NftOrderThirdPartOrderStatusWorker, NftOrderThirdPartOrderStatusWorker>();
        // context.Services.AddSingleton<NftOrderThirdPartNftResultNotifyWorker, NftOrderThirdPartNftResultNotifyWorker>();
        // context.Services.AddSingleton<NftOrderMerchantCallbackWorker, NftOrderMerchantCallbackWorker>();
        // context.Services.AddSingleton<NftOrdersSettlementWorker, NftOrdersSettlementWorker>();
        //
        // context.Services.AddSingleton<NftOrderMerchantCallbackHandler>();
        // context.Services.AddSingleton<NftOrderUpdateHandler>();
        // context.Services.AddSingleton<NftOrderReleaseResultHandler>();
        // context.Services.AddSingleton<NftOrderPaySuccessHandler>();
        // context.Services.AddSingleton<NftOrderTransferHandler>();
        // context.Services.AddSingleton<NftOrderSettlementHandler>();
        // context.Services.AddSingleton<OrderSettlementUpdateHandler>();
        // context.Services.AddSingleton<ThirdPartHandler>();
        context.Services.AddSingleton<IAbpDistributedLock>(GetMockAbpDistributedLock());
        context.Services.AddSingleton(GetMockInMemoryHarness());
        
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAServerApplicationModule>(); });
        // Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CABackGroundModule>(); });
        // Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAServerContractEventHandlerCoreModule>(); });
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

        context.Services.Configure<ActivityTypeOptions>(o =>
        {
            o.TypeMap = new Dictionary<string, string>() { { "TEST", "TEST" } };
            o.TransferTypes = new List<string>() { "TEST", "TransferTypes" };
            o.ContractTypes = new List<string>() { "TEST", "ContractTypes" };
            o.ShowPriceTypes = new List<string>() { "TEST" };
            o.NoShowTypes = new List<string>() { "no show" };
            o.RedPacketTypes = new List<string>() { "no" };
            o.ShowNftTypes = new List<string>() { "TEST" };
            o.TransactionTypeMap = new Dictionary<string, string>() { { "TEST", "TEST" } };
            o.Zero = "0";
        });
        context.Services.Configure<CAServer.Grains.Grain.ApplicationHandler.ChainOptions>(option =>
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
            options.ForestBaseUrl = "http://127.0.0.1";
            options.ExcludedSuffixes = new List<string>();
            options.ExcludedSuffixes.Add("png");
            options.ExcludedSuffixes.Add("jpg");
            options.BucketList = new List<string>();
            options.BucketList.Add("127.0.0.1");
            options.BucketList.Add("127.0.0.1");
            options.BucketList.Add("127.0.0.1");
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
    
    protected IAbpDistributedLock GetMockAbpDistributedLock()
    {
        var mockLockProvider = new Mock<IAbpDistributedLock>();
        var keyRequestTimes = new Dictionary<string, DateTime>();
        mockLockProvider
            .Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns<string, TimeSpan, CancellationToken>((name, timeSpan, cancellationToken) =>
            {
                lock (keyRequestTimes)
                {
                    if (keyRequestTimes.TryGetValue(name, out var lastRequestTime))
                        if ((DateTime.Now - lastRequestTime).TotalMilliseconds <= 1000)
                            return Task.FromResult<IAbpDistributedLockHandle>(null);
                    keyRequestTimes[name] = DateTime.Now;
                    var handleMock = new Mock<IAbpDistributedLockHandle>();
                    handleMock.Setup(h => h.DisposeAsync()).Callback(() =>
                    {
                        lock (keyRequestTimes)
                            keyRequestTimes.Remove(name);
                    });

                    return Task.FromResult(handleMock.Object);
                }
            });

        return mockLockProvider.Object;
    }
    
    protected IBus GetMockInMemoryHarness(params IConsumer[] consumers)
    {
        var busMock = new Mock<IBus>();

        busMock.Setup(bus => bus.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((message, token) =>
            {
                foreach (var consumer in consumers)
                {
                    var consumeMethod = consumer.GetType().GetMethod("Consume");
                    if (consumeMethod == null) continue;
                
                    var consumeContextType = typeof(ConsumeContext<>).MakeGenericType(message.GetType());
                
                    dynamic contextMock = Activator.CreateInstance(typeof(Mock<>).MakeGenericType(consumeContextType));
                    contextMock.SetupGet("Message").Returns(message);
                
                    consumeMethod.Invoke(consumer, new[] { ((Mock)contextMock).Object });
                }
            })
            .Returns(Task.CompletedTask);

        return busMock.Object;
    }
}
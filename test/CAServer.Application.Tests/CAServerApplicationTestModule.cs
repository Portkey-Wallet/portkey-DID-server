using System;
using System.Collections.Generic;
using System.Configuration;
using CAServer.Grain.Tests;
using CAServer.Hub;
using CAServer.IpInfo;
using CAServer.Options;
using CAServer.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute.Extensions;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

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
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        // context.Services.AddSingleton(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.AddSingleton<ISearchAppService, SearchAppService>();
        context.Services.AddSingleton<IConnectionProvider, ConnectionProvider>();
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
        context.Services.Configure<TokenListOptions>(o => { o.UserToken = tokenList; });
        // context.Services.Configure<IpServiceSettingOptions>(o =>
        // {
        //     o.ExpirationDays = 1;
        //     o.BaseUrl = "http://127.0.0.1:6889";
        //     o.Language = "zh";
        //     o.AccessKey = "";
        // });
        context.Services.Configure<IpServiceSettingOptions>(o => o.ExpirationDays = 1);

        // context.Services.AddTransient<IDistributedEventHandler<UserTokenEto>>(sp =>
        //     sp.GetService<UserTokenEntityHandler>());

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
            option.PrivateKey = "28d2520e2c480ef6f42c2803dcf4348807491237fd294c0f0a3d7c8f9ab8fb91";
        });
        base.ConfigureServices(context);
    }
}
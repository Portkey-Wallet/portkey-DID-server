using System.Collections.Generic;
using CAServer.CAActivity.Provider;
using CAServer.EntityEventHandler.Core;
using CAServer.Options;
using CAServer.Orleans.TestBase;
using CAServer.Tokens.Etos;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Modularity;

namespace CAServer;

[DependsOn(
    typeof(CAServerApplicationModule),
    typeof(CAServerDomainTestModule),
    typeof(CAServerOrleansTestBaseModule),
    typeof(AbpEventBusModule),
    typeof(CAServerEntityEventHandlerCoreModule)
)]
public class CAServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton(new GraphQLHttpClient("http://192.168.67.84:8083/AElfIndexer_DApp/PortKeyIndexerCASchema/graphql", new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
        context.Services.AddSingleton<IActivityProvider, ActivityProvider>();
        context.Services.AddSingleton<IUserAssetsProvider, UserAssetsProvider>();
        context.Services.AddSingleton<IUserAssetsAppService, UserAssetsAppService>();
        var tokenList = new List<UserTokenItem>();
        var token1 = new UserTokenItem
        {
            IsDefault = true,
            IsDisplay = true,
            SortWeight = 1,
            Token = new Options.Token
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
            Token = new Options.Token
            {
                ChainId = "TDVV",
                Address = "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx",
                Decimals = 8,
                Symbol = "ELF"
            }
        };
        tokenList.Add(token1);
        tokenList.Add(token2);
        context.Services.Configure<TokenListOptions>(o =>
        {
            o.UserToken = tokenList;
        });
        context.Services.AddTransient<IDistributedEventHandler<UserTokenEto>>(sp =>
            sp.GetService<UserTokenEntityHandler>());
        base.ConfigureServices(context);
    }
}
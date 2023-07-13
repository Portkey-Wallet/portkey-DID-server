using AElf.Indexing.Elasticsearch.Options;
using CAServer.EntityEventHandler.Core;
using CAServer.EntityEventHandler.Tests.Token;
using CAServer.Options;
using CAServer.Orleans.TestBase;
using CAServer.Search;
using CAServer.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.AutoMapper;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Modularity;

namespace CAServer.EntityEventHandler.Tests;


[DependsOn(
    typeof(CAServerEntityEventHandlerCoreModule),
    typeof(CAServerOrleansTestBaseModule),
    typeof(CAServerDomainTestModule),
    typeof(CAServerApplicationContractsModule))]
public class CAServerEntityEventHandlerTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<CAServerEntityEventHandlerCoreModule>();
        });
        context.Services.AddSingleton<IUserTokenAppService, UserTokenAppService>();
        context.Services.AddSingleton<ISearchAppService, MockSearchAppService>();
        context.Services.AddSingleton<ISearchService, UserTokenSearchService>();
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
        context.Services.Configure<IndexSettingOptions>(o =>
        {
            o.IndexPrefix = "caservertest";
        });
    }

}
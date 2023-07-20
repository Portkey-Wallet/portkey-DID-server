using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch.Options;
using CAServer.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NSubstitute;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.Search;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class SearchAppServiceTest : CAServerApplicationTestBase
{
    protected ICurrentUser _currentUser;
    protected ISearchAppService _searchAppService;

    public SearchAppServiceTest()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        _currentUser = Substitute.For<ICurrentUser>();
        services.AddSingleton(_currentUser);
        services.AddSingleton(GetIndexSettingOptions());
        services.AddSingleton(GetEsIndexBlacklistOptions());
        services.AddSingleton<ISearchService, UserTokenSearchService>();
        services.AddSingleton<ISearchService, ContactSearchService>();
        services.AddSingleton<ISearchService, ChainsInfoSearchService>();

        services.AddSingleton<ISearchService, AccountRecoverySearchService>();
        services.AddSingleton<ISearchService, AccountRegisterSearchService>();
        services.AddSingleton<ISearchService, CAHolderSearchService>();
        services.AddSingleton<ISearchService, OrderSearchService>();
        services.AddSingleton<ISearchService, NotifySearchService>();
    }

    [Fact]
    public async void GetListByLucenceAsyncTest()
    {
        Login(Guid.NewGuid());

        var list = new List<string>
        {
            "usertokenindex", "contactindex", "chainsinfoindex", "accountrecoverindex",
            "accountregisterindex", "ramporderindex", "notifyrulesindex"
        };
        foreach (var indexName in list)
        {
            var res = await _searchAppService.GetListByLucenceAsync(indexName, new GetListInput
            {
                SkipCount = 0,
                MaxResultCount = 11,
                Sort = "any"
            });
            var pageDto = JsonConvert.DeserializeObject<PagedResultDto<object>>(res);
            pageDto.TotalCount.ShouldBe(-1);
        }
    }

    [Fact]
    public async void GetListByLucenceAsync_Blacklist_Test()
    {
        var indexName = "caholderindex";
        try
        {
            await _searchAppService.GetListByLucenceAsync(indexName, new GetListInput
            {
                SkipCount = 0,
                MaxResultCount = 11,
                Sort = "any"
            });
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("Not allowed.");
        }
       
    }

    private void Login(Guid userId)
    {
        _currentUser.Id.Returns(userId);
        _currentUser.IsAuthenticated.Returns(true);
    }

    private IOptionsSnapshot<IndexSettingOptions> GetIndexSettingOptions()
    {
        var mock = new Mock<IOptionsSnapshot<IndexSettingOptions>>();
        mock.Setup(m => m.Value).Returns(
            new IndexSettingOptions
            {
                IndexPrefix = "test"
            }
        );
        return mock.Object;
    }
    
    private IOptionsSnapshot<EsIndexBlacklistOptions> GetEsIndexBlacklistOptions()
    {
        var mock = new Mock<IOptionsSnapshot<EsIndexBlacklistOptions>>();
        mock.Setup(m => m.Value).Returns(
            new EsIndexBlacklistOptions
            {
                Indexes = new List<string>(){"caholderindex"}
            }
        );
        return mock.Object;
    }
}
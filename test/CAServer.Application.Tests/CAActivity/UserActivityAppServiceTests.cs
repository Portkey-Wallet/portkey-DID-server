using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAActivity.Dto;
using CAServer.CAActivity.Dtos;
using CAServer.UserAssets;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.CAActivity;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class UserActivityAppServiceTests : CAServerApplicationTestBase
{
    protected ICurrentUser _currentUser;
    protected IUserActivityAppService _userActivityAppService;

    public UserActivityAppServiceTests()
    {
        _userActivityAppService = GetRequiredService<IUserActivityAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        _currentUser = Substitute.For<ICurrentUser>();
        services.AddSingleton(_currentUser);
        services.AddSingleton(GetMockActivityProvider());
        services.AddSingleton(GetMockTokenAppService());
        services.AddSingleton(GetUserContactProvider());
        services.AddSingleton(GetActivitiesIcon());
    }
    
    private void Login(Guid userId)
    {
        _currentUser.Id.Returns(userId);
        _currentUser.IsAuthenticated.Returns(true);
    }

    [Fact]
    public async Task GetActivityTest()
    {
        Login(Guid.NewGuid());
        var param = new GetActivityRequestDto
        {
            BlockHash = "blockHash",
            TransactionId = "transactionId",
            CaAddresses = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" }
        };

        var result = await _userActivityAppService.GetActivityAsync(param);
        result.TransactionType.ShouldBe("methodName");
        result.TransactionFees.First().FeeInUsd.ShouldBe(200.ToString());
        result.TransactionFees.First().Decimals.ShouldBe("8");
    }

    [Fact]
    public async Task GetActivitiesTest()
    {
        var param = new GetActivitiesRequestDto
        {
            SkipCount = 0,
            MaxResultCount = 10,
            CaAddresses = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" }
        };

        var result = await _userActivityAppService.GetActivitiesAsync(param);
        result.TotalRecordCount.ShouldBe(1);

        var data = result.Data[0];
        data.TransactionType.ShouldBe("methodName");
        data.TransactionFees.First().FeeInUsd.ShouldBe(200.ToString());
        data.TransactionFees.First().Decimals.ShouldBe("8");
    }
}
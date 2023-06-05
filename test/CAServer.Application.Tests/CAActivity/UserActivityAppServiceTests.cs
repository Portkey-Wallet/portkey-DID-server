using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAActivity.Dto;
using CAServer.CAActivity.Dtos;
using CAServer.CAActivity.Provider;
using CAServer.UserAssets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using NSubstitute;
using Shouldly;
using Volo.Abp.Users;
using Volo.Abp.Validation;
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
        services.AddSingleton(GetMockTokenAppService());
        services.AddSingleton(GetUserContactProvider());
        services.AddSingleton(GetActivitiesIcon());
        services.AddSingleton(GetMockActivityProvider());
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
        //result.TransactionFees.First().FeeInUsd.ShouldBe("0.000002");
        result.TransactionFees.First().Decimals.ShouldBe("8");
    }

    [Fact]
    public async Task GetActivity_TransactionId_NullTest()
    {
        Login(Guid.NewGuid());

        var token = new TokenDto
        {
            Symbol = string.Empty,
            Address = string.Empty
        };

        try
        {
            var param = new GetActivityRequestDto
            {
                BlockHash = "blockHash",
                TransactionId = "",
                CaAddresses = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" }
            };

            await _userActivityAppService.GetActivityAsync(param);
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task GetActivity_BlockHash_Null_Test()
    {
        Login(Guid.NewGuid());
        try
        {
            var param = new GetActivityRequestDto
            {
                BlockHash = "",
                TransactionId = "TransactionId",
                CaAddresses = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" }
            };

            await _userActivityAppService.GetActivityAsync(param);
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task GetActivity_CaAddresses_Null_Test()
    {
        Login(Guid.NewGuid());
        try
        {
            var param = new GetActivityRequestDto
            {
                BlockHash = "BlockHash",
                TransactionId = "TransactionId",
                CaAddresses = null
            };

            await _userActivityAppService.GetActivityAsync(param);
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task GetTwoCaTransactionsTest()
    {
        Login(Guid.NewGuid());
        var param = new GetTwoCaTransactionRequestDto
        {
            TargetAddressInfos = new List<CAAddressInfo>()
            {
                new CAAddressInfo()
                {
                    ChainId = "test",
                    CaAddress = "CaAddress"
                }
            },
            CaAddressInfos = new List<CAAddressInfo>()
            {
                new CAAddressInfo()
                {
                    ChainId = "test",
                    CaAddress = "CaAddress"
                }
            }
        };

        var result = await _userActivityAppService.GetTwoCaTransactionsAsync(param);
        result.ShouldNotBeNull();
        result.TotalRecordCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetTwoCaTransactions_Param_Empty_Test()
    {
        Login(Guid.NewGuid());
        var param = new GetTwoCaTransactionRequestDto();
        var info = new TransferInfo
        {
            FromAddress = string.Empty,
            ToAddress = string.Empty,
            ToChainId = string.Empty,
            FromChainId = string.Empty,
            FromCAAddress = string.Empty
        };

        try
        {
            var detail = new NftDetail
            {
                ImageUrl = string.Empty,
                Alias = string.Empty,
                NftId = string.Empty
            };
            var result = await _userActivityAppService.GetTwoCaTransactionsAsync(param);
        }
        catch (Exception e)
        {
            e.Message.ShouldContain("must be non-empty");
        }
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
        //data.TransactionFees.First().FeeInUsd.ShouldBe("0.000002");
        data.TransactionFees.First().Decimals.ShouldBe("8");
    }
}
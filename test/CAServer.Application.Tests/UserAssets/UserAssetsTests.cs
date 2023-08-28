using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.UserAssets.Dtos;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.UserAssets;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class UserAssetsTests : CAServerApplicationTestBase
{
    protected ICurrentUser _currentUser;
    protected IUserAssetsAppService _userAssetsAppService;

    public UserAssetsTests()
    {
        _userAssetsAppService = GetRequiredService<IUserAssetsAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        _currentUser = Substitute.For<ICurrentUser>();
        services.AddSingleton(_currentUser);
        services.AddSingleton(GetMockUserAssetsProvider());
        services.AddSingleton(GetMockTokenAppService());
        services.AddSingleton(GetUserContactProvider());
        services.AddSingleton(GetMockTokenInfoOptions());
        services.AddSingleton(GetContractProvider());
    }

    private void Login(Guid userId)
    {
        _currentUser.Id.Returns(userId);
        _currentUser.IsAuthenticated.Returns(true);
    }

    [Fact]
    public async Task GetTokenTest()
    {
        Login(Guid.NewGuid());
        var param = new GetTokenRequestDto
        {
            SkipCount = 0,
            MaxResultCount = 10,
            CaAddresses = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" }
        };


        var result = await _userAssetsAppService.GetTokenAsync(param);
        result.TotalRecordCount.ShouldBe(3);

        var data = result.Data.First();
        data.Balance.ShouldBe(1000.ToString());
        data.Symbol.ShouldBe("ELF");
        data.ChainId.ShouldBe("AELF");
        data.BalanceInUsd.ShouldBe("0.00002");
    }

    [Fact]
    public async Task GetNftProtocolsTest()
    {
        Login(Guid.NewGuid());
        var param = new GetNftCollectionsRequestDto
        {
            SkipCount = 0,
            MaxResultCount = 10,
            CaAddresses = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" }
        };

        var result = await _userAssetsAppService.GetNFTCollectionsAsync(param);
        result.TotalRecordCount.ShouldBe(1);

        var data = result.Data.First();
        data.Symbol.ShouldBe("TEST-0");
        data.ChainId.ShouldBe("AELF");
    }

    [Fact]
    public async Task GetNftItemsTest()
    {
        Login(Guid.NewGuid());
        var param = new GetNftItemsRequestDto
        {
            SkipCount = 0,
            MaxResultCount = 10,
            CaAddresses = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" },
            Symbol = "TEST-0"
        };

        var result = await _userAssetsAppService.GetNFTItemsAsync(param);
        result.TotalRecordCount.ShouldBe(1);

        var data = result.Data.First();
        data.Symbol.ShouldBe("TEST-1");
        data.ChainId.ShouldBe("AELF");
    }

    [Fact]
    public async Task GetRecentTransactionUsersTest()
    {
        Login(Guid.NewGuid());
        var param = new GetRecentTransactionUsersRequestDto
        {
            SkipCount = 0,
            MaxResultCount = 10,
            CaAddresses = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" }
        };

        var result = await _userAssetsAppService.GetRecentTransactionUsersAsync(param);
        result.TotalRecordCount.ShouldBe(1);

        var data = result.Data.First();
        data.ChainId.ShouldBe("AELF");
        data.Name.ShouldBe("test");
    }

    [Fact]
    public async Task SearchUserAssetsTest()
    {
        Login(Guid.NewGuid());
        var param = new SearchUserAssetsRequestDto
        {
            SkipCount = 0,
            MaxResultCount = 10,
            Keyword = "ELF",
            CaAddresses = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" }
        };

        var result = await _userAssetsAppService.SearchUserAssetsAsync(param);
        result.TotalRecordCount.ShouldBe(1);

        var data = result.Data.First();
        data.Symbol.ShouldBe("ELF");
        data.ChainId.ShouldBe("AELF");
        data.TokenInfo.BalanceInUsd.ShouldBe("0.00002");
    }

    [Fact]
    public async Task GetSymbolImagesAsyncTest()
    {
        Login(Guid.NewGuid());
        var result = _userAssetsAppService.GetSymbolImagesAsync();
        result.SymbolImages.Count().ShouldBe(1);
        var data = result.SymbolImages.First();
        data.Key.ShouldBe("ELF");
        data.Value.ShouldBe("ImageUrl");
    }

    [Fact]
    public async Task GetTokenBalanceAsyncTest()
    {
        Login(Guid.NewGuid());
        var input = new GetTokenBalanceRequestDto
        {
            CaAddress = "a8ae393ecb7cba148d269c262993eacb6a1b25b4dc55270b55a9be7fc2412033",
            Symbol = "ELF"
        };
        
        var result = await _userAssetsAppService.GetTokenBalanceAsync(input);
        result.Balance.ShouldBe("2000");
        
        
    }
    
   
    
    
    
}
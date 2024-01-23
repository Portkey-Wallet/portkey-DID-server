using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Contacts.Provider;
using CAServer.Guardian.Provider;
using CAServer.Options;
using CAServer.Tokens;
using CAServer.Tokens.Provider;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute;
using Orleans;
using Shouldly;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;
using Xunit;
using Token = CAServer.UserAssets.Dtos.Token;

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

    public IUserAssetsAppService GetMock()
    {
        var loggerMock = new Mock<ILogger<UserAssetsAppService>>();
        var tokenAppServiceMock = new Mock<ITokenAppService>();
        var userAssetsProviderMock = new Mock<IUserAssetsProvider>();
        var userContactProviderMock = new Mock<IUserContactProvider>();
        var tokenInfoOptionsMock = new Mock<IOptions<TokenInfoOptions>>();
        tokenInfoOptionsMock.Setup(o => o.Value).Returns(new TokenInfoOptions());
        var imageProcessProviderMock = new Mock<IImageProcessProvider>();
        var chainOptionsMock = new Mock<IOptions<ChainOptions>>();
        chainOptionsMock.Setup(o => o.Value).Returns(new ChainOptions());
        var contractProviderMock = new Mock<IContractProvider>();
        var contactProviderMock = new Mock<IContactProvider>();
        var clusterClientMock = new Mock<IClusterClient>();
        var guardianProviderMock = new Mock<IGuardianProvider>();
        var seedImageOptionsMock = new Mock<IOptionsSnapshot<SeedImageOptions>>();
        seedImageOptionsMock.Setup(o => o.Value).Returns(new SeedImageOptions());
        var userTokenAppServiceMock = new Mock<IUserTokenAppService>();
        var tokenProvider = new Mock<ITokenProvider>();
        var userAssetsAppService = new UserAssetsAppService(
            logger: loggerMock.Object,
            userAssetsProvider: userAssetsProviderMock.Object,
            tokenAppService: tokenAppServiceMock.Object,
            userContactProvider: userContactProviderMock.Object,
            tokenInfoOptions: tokenInfoOptionsMock.Object,
            imageProcessProvider: imageProcessProviderMock.Object,
            chainOptions: chainOptionsMock.Object,
            contractProvider: contractProviderMock.Object,
            distributedEventBus: GetRequiredService<IDistributedEventBus>(),
            seedImageOptions:  seedImageOptionsMock.Object,
            userTokenAppService: userTokenAppServiceMock.Object,
            tokenProvider: tokenProvider.Object,
            assetsLibraryProvider: GetRequiredService<IAssetsLibraryProvider>(), 
            userTokenCache: GetRequiredService<IDistributedCache<List<Token>>>());
        return userAssetsAppService;
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
        services.AddSingleton(GetMockAssetsInfoOptions());
        services.AddSingleton(GetContractProvider());
        services.AddSingleton(GetMockSeedImageOptions());
        services.AddSingleton(GetMockTokenProvider());
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
            CaAddressInfos = new List<CAAddressInfo>()
            {
                new CAAddressInfo()
                {
                    CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                    ChainId = "AELF"
                }
            }
        };


        var result = await _userAssetsAppService.GetTokenAsync(param);
        // result.TotalRecordCount.ShouldBe(2);
        //
        // var data = result.Data.First();
        // data.Balance.ShouldBe(1000.ToString());
        // data.Symbol.ShouldBe("ELF");
        // data.ChainId.ShouldBe("AELF");
        // data.BalanceInUsd.ShouldBe("0.00002");
    }

    [Fact]
    public async Task GetNftProtocolsTest()
    {
        Login(Guid.NewGuid());
        var param = new GetNftCollectionsRequestDto
        {
            SkipCount = 0,
            MaxResultCount = 10,
            CaAddressInfos = new List<CAAddressInfo>()
            {
                new CAAddressInfo()
                {
                    CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                    ChainId = "AELF"
                }
            }
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
            CaAddressInfos = new List<CAAddressInfo>()
            {
                new CAAddressInfo()
                {
                    CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                    ChainId = "AELF"
                }
            },
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
            CaAddressInfos = new List<CAAddressInfo>()
            {
                new CAAddressInfo()
                {
                    CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                    ChainId = "AELF"
                }
            }
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
            CaAddressInfos = new List<CAAddressInfo>()
            {
                new CAAddressInfo()
                {
                    CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                    ChainId = "AELF"
                }
            }
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
        result.Balance.ShouldBe(null);
    }
    
   
    
    
    
}
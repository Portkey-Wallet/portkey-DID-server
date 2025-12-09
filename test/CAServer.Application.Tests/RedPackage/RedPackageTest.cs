using System;
using System.Threading.Tasks;
using CAServer.EnumType;
using CAServer.Options;
using CAServer.RedPackage.Dtos;
using CAServer.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.RedPackage;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class RedPackageTest : CAServerApplicationTestBase
{
    private readonly IRedPackageAppService _redPackageAppService;
    private readonly IdentityUserManager _userManager;
    protected ICurrentUser _currentUser;
    private readonly Guid userId = Guid.Parse("158ff364-3264-4234-ab20-02aaada2aaad");
    private Guid redPackageId = Guid.Parse("f825f8f1-d3a4-4ee7-a98d-ad06b61094c0");
    
    public RedPackageTest()
    {
        _redPackageAppService = GetRequiredService<IRedPackageAppService>();
        _userManager = GetRequiredService<IdentityUserManager>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        _currentUser = Substitute.For<ICurrentUser>();
        services.AddSingleton(_currentUser);
        services.AddSingleton(GetMockClusterClient());
        services.AddSingleton(GetMockHttpContextAccessor());
        services.AddSingleton(MockChainOptionsSnapshot());
        services.AddSingleton(MockRedPackageKeyGrain());
        services.AddSingleton(MockRedpackageOptions());
        services.AddSingleton(MockCryptoBoxGrain());
        services.AddSingleton(MockRedPackageIndex());
        services.AddSingleton(MockGraphQlOptions());
        services.AddSingleton(GetMockIGraphQLHelper());
        
        services.AddSingleton(TokenAppServiceTest.GetMockCoinGeckoOptions());
        services.AddSingleton(TokenAppServiceTest.GetMockTokenPriceWorkerOption());
        services.AddSingleton(TokenAppServiceTest.GetMockSignatureServerOptions());
        services.AddSingleton(TokenAppServiceTest.GetMockRequestLimitProvider());
        services.AddSingleton(TokenAppServiceTest.GetMockSecretProvider());
    }
    
    protected new IOptionsSnapshot<GraphQLOptions> MockGraphQlOptions()
    {
        var options = new GraphQLOptions()
        {
            Configuration = "http://127.0.0.1:9200/AElfIndexer_DApp/PortKeyIndexerCASchema/graphq"
        };

        var mock = new Mock<IOptionsSnapshot<GraphQLOptions>>();
        mock.Setup(o => o.Value).Returns(options);
        return mock.Object;
    }
    
    // [Fact]
    public async Task GenerateRedPackageAsync_test()
    {
        Login(userId);
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _redPackageAppService.GenerateRedPackageAsync(new GenerateRedPackageInputDto()
            {
                ChainId = "xxx",
                Symbol = "xxxx"
            });
        });
        
        var res = await _redPackageAppService.GenerateRedPackageAsync(new GenerateRedPackageInputDto()
        {
            ChainId = "AELF",
            Symbol = "ELF"
        });
        res.ChainId.ShouldBe("AELF");
        res.Symbol.ShouldBe("ELF");
    }
    
    // [Fact]
    public async Task SendRedPackageAsync_test()
    {
        var redPackage = await _redPackageAppService.GenerateRedPackageAsync(new GenerateRedPackageInputDto()
        {
            ChainId = "AELF",
            Symbol = "ELF"
        });
        redPackageId = redPackage.Id;
        var input = NewSendRedPackageInputDto();
        input.ChainId = "AELF";
        input.Symbol = "XXX";
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _redPackageAppService.SendRedPackageAsync(input);
        });
        
        //min error
        input = NewSendRedPackageInputDto();
        input.TotalAmount = "1";
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _redPackageAppService.SendRedPackageAsync(input);
        });
        
        //type  error
        input = NewSendRedPackageInputDto();
        input.Type = 0;
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _redPackageAppService.SendRedPackageAsync(input);
        });
        
        //id error
        input = NewSendRedPackageInputDto();
        input.Id = Guid.Empty;
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _redPackageAppService.SendRedPackageAsync(input);
        });
        
        //count error
        input = NewSendRedPackageInputDto();
        input.Count = 0;
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _redPackageAppService.SendRedPackageAsync(input);
        });
        
        //amount error
        input = NewSendRedPackageInputDto();
        input.TotalAmount = "10";
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _redPackageAppService.SendRedPackageAsync(input);
        });
        
        //count error
        input = NewSendRedPackageInputDto();
        input.Count = 2000;
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _redPackageAppService.SendRedPackageAsync(input);
        });
        
        //chain id error
        input = NewSendRedPackageInputDto();
        input.ChainId = "AELF-1";
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _redPackageAppService.SendRedPackageAsync(input);
        });
        
        //uid error
        input = NewSendRedPackageInputDto();
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _redPackageAppService.SendRedPackageAsync(input);
        });
        
        Login(userId);
        input = NewSendRedPackageInputDto();
        var result = await _redPackageAppService.SendRedPackageAsync(input);
        result.SessionId.ShouldNotBe(Guid.Empty);
        
        //get result
        var res = await _redPackageAppService.GetCreationResultAsync(result.SessionId);
        res.Status.ShouldBe(RedPackageTransactionStatus.Processing);
    }

    // [Fact]
    public async Task GetCreationResultAsync_test()
    {
        var res = await _redPackageAppService.GetCreationResultAsync(Guid.Parse("1f691ad9-1a99-4456-b4d4-fdfc3cd128a2"));
        res.Status.ShouldBe(RedPackageTransactionStatus.Fail);
    }
    
    // [Fact]
    public async Task GetRedPackageDetailAsync_test()
    {
        var res = await _redPackageAppService.GetRedPackageDetailAsync(redPackageId, RedPackageDisplayType.Common, 0, 0);
        res.Id.ShouldBe(Guid.Empty);
        
        Login(userId);
        var input = NewSendRedPackageInputDto();
        await _redPackageAppService.SendRedPackageAsync(input);
        res = await _redPackageAppService.GetRedPackageDetailAsync(redPackageId, RedPackageDisplayType.Common, -1, -1);
        res.Id.ShouldBe(redPackageId);
    }

    // [Fact]
    public async Task GetRedPackageConfigAsync_test()
    {
        var res = await _redPackageAppService.GetRedPackageConfigAsync(null, null);
        res.TokenInfo.Count.ShouldBeGreaterThan(0);
        
        res = await _redPackageAppService.GetRedPackageConfigAsync("AELF", "ELF");
        res.TokenInfo.Count.ShouldBeGreaterThan(0);
        
        res = await _redPackageAppService.GetRedPackageConfigAsync("AELF-1", "ELF");
        res.TokenInfo.Count.ShouldBe(0);
    }
    
    // [Fact]
    public async Task GrabRedPackageAsync_test()
    {
        var input = new GrabRedPackageInputDto()
        {
            Id = redPackageId,
            UserCaAddress = "xxxxxxx"
        };
        
        var res = await _redPackageAppService.GrabRedPackageAsync(input);
        res.Result.ShouldBe(RedPackageGrabStatus.Fail);
        
        Login(userId);
        var sendinput = NewSendRedPackageInputDto();
        await _redPackageAppService.SendRedPackageAsync(sendinput);
        // res = await _redPackageAppService.GrabRedPackageAsync(input);
        // res.Result.ShouldBe(RedPackageGrabStatus.Success);
        // var detailDto = await _redPackageAppService.GetRedPackageDetailAsync(redPackageId, 0, 10);
        // detailDto.Status.ShouldBe(RedPackageStatus.Claimed);

        var newId = Guid.NewGuid();
        sendinput.Id = newId;
        sendinput.TotalAmount = "100";
        sendinput.Count = 10;
        sendinput.Type = RedPackageType.Fixed;
        await _redPackageAppService.SendRedPackageAsync(sendinput);
        input.Id = newId;
        res = await _redPackageAppService.GrabRedPackageAsync(input);
        res.Result.ShouldBe(RedPackageGrabStatus.Success);
        res.Amount.ShouldBe("10");
    }
    
    private void Login(Guid userId)
    {
        _currentUser.Id.Returns(userId);
        _currentUser.IsAuthenticated.Returns(true);
    }

    private SendRedPackageInputDto NewSendRedPackageInputDto()
    {
        return new SendRedPackageInputDto()
        {
            Id = redPackageId,
            Type = RedPackageType.Random,
            Count = 500,
            TotalAmount = "1000000",
            Memo = "xxxx",
            ChainId = "AELF",
            Symbol = "ELF",
            ChannelUuid = "xxxx",
            //SendUuid = "xxx",
            RawTransaction = "xxxxx",
            Message = "xxxx"
        };
    }
}
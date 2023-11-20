using System;
using System.Threading.Tasks;
using CAServer.RedPackage.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.RedPackage;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class RedPackageTest : CAServerApplicationTestBase
{
    private readonly IRedPackageAppService _redPackageAppService;
    protected ICurrentUser _currentUser;
    private readonly Guid userId = Guid.NewGuid();
    private readonly Guid redPackageId = Guid.NewGuid();
    
    public RedPackageTest()
    {
        _redPackageAppService = GetRequiredService<IRedPackageAppService>();;
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        _currentUser = Substitute.For<ICurrentUser>();
        services.AddSingleton(_currentUser);
        services.AddSingleton(GetMockHttpContextAccessor());
    }
    
    [Fact]
    public async Task GenerateRedPackageAsync_test()
    {
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
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
    
    [Fact]
    public async Task SendRedPackageAsync_test()
    {
        var input = NewSendRedPackageInputDto();
        input.ChainId = "AELF";
        input.Symbol = "XXX";
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
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
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
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

    [Fact]
    public async Task GetCreationResultAsync_test()
    {
        var res = await _redPackageAppService.GetCreationResultAsync(Guid.NewGuid());
        res.Status.ShouldBe(RedPackageTransactionStatus.Fail);
    }
    
    [Fact]
    public async Task GetRedPackageDetailAsync_test()
    {
        var res = await _redPackageAppService.GetRedPackageDetailAsync(redPackageId, 0, 0);
        res.Id.ShouldBe(Guid.Empty);
        
        Login(userId);
        var input = NewSendRedPackageInputDto();
        await _redPackageAppService.SendRedPackageAsync(input);
        res = await _redPackageAppService.GetRedPackageDetailAsync(redPackageId, -1, -1);
        res.Id.ShouldBe(redPackageId);
    }

    [Fact]
    public async Task GetRedPackageConfigAsync_test()
    {
        var res = await _redPackageAppService.GetRedPackageConfigAsync(null, null);
        res.TokenInfo.Count.ShouldBeGreaterThan(0);
        
        res = await _redPackageAppService.GetRedPackageConfigAsync("AELF", "ELF");
        res.TokenInfo.Count.ShouldBeGreaterThan(0);
        
        res = await _redPackageAppService.GetRedPackageConfigAsync("AELF-1", "ELF");
        res.TokenInfo.Count.ShouldBe(0);
    }
    
    [Fact]
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
        res = await _redPackageAppService.GrabRedPackageAsync(input);
        res.Result.ShouldBe(RedPackageGrabStatus.Success);
        var detailDto = await _redPackageAppService.GetRedPackageDetailAsync(redPackageId, 0, 10);
        detailDto.GrabbedAmount.ShouldBe(res.Amount);
        detailDto.Grabbed.ShouldBe(1);
        detailDto.Status.ShouldBe(RedPackageStatus.Claimed);
        detailDto.Items.Count.ShouldBe(1);
        
        res = await _redPackageAppService.GrabRedPackageAsync(input);
        res.Result.ShouldBe(RedPackageGrabStatus.Fail);

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
            SendUuid = "xxx",
            RawTransaction = "xxxxx",
            Message = "xxxx"
        };
    }
}
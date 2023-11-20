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
        input.TotalAmount = 1;
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
        input.TotalAmount = 10;
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
        
        var uid = Guid.NewGuid();
        Login(uid);
        input = NewSendRedPackageInputDto();
        var result = await _redPackageAppService.SendRedPackageAsync(input);
        result.SessionId.ShouldNotBe(Guid.Empty);
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
            Id = Guid.NewGuid(),
            Type = RedPackageType.Random,
            Count = 500,
            TotalAmount = 1000000,
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
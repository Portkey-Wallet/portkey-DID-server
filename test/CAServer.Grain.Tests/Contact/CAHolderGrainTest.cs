using CAServer.CAAccount.Dtos;
using CAServer.Grains.Grain.Contacts;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.Grain.Tests.Contact;

public class CAHolderGrainTest : CAServerGrainTestBase
{
    protected ICurrentUser _currentUser;

    public CAHolderGrainTest()
    {
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        _currentUser = Substitute.For<ICurrentUser>();
        services.AddSingleton(_currentUser);
    }

    private void Login(Guid userId)
    {
        _currentUser.Id.Returns(userId);
        _currentUser.IsAuthenticated.Returns(true);
    }

    [Fact]
    public async void AddHolderAsyncTest()
    {
        var userId = Guid.NewGuid();
        var cAHolderGrainDto = new CAHolderGrainDto
        {
            UserId = userId,
            CreateTime = DateTimeOffset.UtcNow.DateTime,
            CaHash = "1212212",
            Nickname = "666"
        };
        var grain = Cluster.Client.GetGrain<ICAHolderGrain>(userId);

        var newName = "7777";
        var updateDto = await grain.UpdateNicknameAsync(newName);
        updateDto.Success.ShouldBe(false);
        updateDto.Message.ShouldBe(CAHolderMessage.NotExistMessage);

        var res = await grain.AddHolderAsync(cAHolderGrainDto);
        res.Success.ShouldBe(true);
        res.Data.Nickname.ShouldBe(cAHolderGrainDto.Nickname);

        res = await grain.AddHolderAsync(cAHolderGrainDto);
        res.Success.ShouldBe(false);
        res.Message.ShouldBe(CAHolderMessage.ExistedMessage);

        updateDto = await grain.UpdateNicknameAsync(newName);
        updateDto.Success.ShouldBe(true);
        updateDto.Data.Nickname.ShouldBe(newName);

        var newHolderName = "Tom";
        var avatar = "Tom-avatar";
        var holderInfo = await grain.UpdateHolderInfo(new HolderInfoDto()
        {
            Avatar = avatar,
            NickName = newHolderName
        });
        
        holderInfo.Success.ShouldBeTrue();
        holderInfo.Data.Nickname.ShouldBe(newHolderName);
        holderInfo.Data.Avatar.ShouldBe(avatar);
    }
}
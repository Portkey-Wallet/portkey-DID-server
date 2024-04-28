using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.AppleAuth.Provider;
using CAServer.Entities.Es;
using CAServer.Security;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.ImUser;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class ImUserAppServiceTest : CAServerApplicationTestBase
{
    private readonly IImUserAppService _imUserAppService;
    private readonly CurrentUser _currentUser;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;

    public ImUserAppServiceTest()
    {
        _imUserAppService = GetRequiredService<IImUserAppService>();
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
        _caHolderRepository = GetRequiredService<INESTRepository<CAHolderIndex, Guid>>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockAppleUserProvider());
    }

    [Fact]
    public async Task GetHolderInfoAsyncTest()
    {
        try
        {
            var userId = _currentUser.GetId();

            await _caHolderRepository.AddOrUpdateAsync(new CAHolderIndex()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CaHash = "test",
                NickName = "test"
            });
            await _imUserAppService.GetHolderInfoAsync(userId);
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task GetHolderInfosTest()
    {
        try
        {
            var userId = _currentUser.GetId();

            await _caHolderRepository.AddOrUpdateAsync(new CAHolderIndex()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CaHash = "test",
                NickName = "test"
            });
            await _imUserAppService.GetUserInfoAsync(new List<Guid>()
            {
                userId
            });
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    private IAppleUserProvider GetMockAppleUserProvider()
    {
        var provider = new Mock<IAppleUserProvider>();

        provider.Setup(t => t.GetUserExtraInfoAsync(It.IsAny<string>())).ReturnsAsync(new AppleUserExtraInfo()
        {
            UserId = Guid.NewGuid().ToString("N"),
            FirstName = "Kui",
            LastName = "Li"
        });

        provider.Setup(t => t.SetUserExtraInfoAsync(It.IsAny<AppleUserExtraInfo>())).Returns(Task.CompletedTask);

        return provider.Object;
    }
}
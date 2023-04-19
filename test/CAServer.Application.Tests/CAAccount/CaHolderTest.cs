using System;
using System.Threading.Tasks;
using CAServer.Dtos;
using CAServer.Grain.Tests;
using CAServer.Grains.Grain.Chain;
using CAServer.Grains.Grain.Contacts;
using CAServer.Security;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSubstitute;
using OpenIddict.Abstractions;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp.Security.Claims;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.CAAccount;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class CaHolderTest : CAServerApplicationTestBase
{
    private readonly INickNameAppService _nickNameAppService;
    private ICurrentUser _currentUser;
    private readonly TestCluster _cluster;
    
    public CaHolderTest()
    {
        _nickNameAppService = GetRequiredService<INickNameAppService>();
        _cluster = GetRequiredService<ClusterFixture>().Cluster;
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
        services.AddSingleton(_currentUser);
    }

    [Fact]
    public async Task SetNicknameTest()
    {
        var grain = _cluster.Client.GetGrain<ICAHolderGrain>(_currentUser.GetId());
        await grain.AddHolderAsync(new CAHolderGrainDto
        {
            UserId = _currentUser.GetId(),
            CaHash = "hash"
        });

        var result = await _nickNameAppService.SetNicknameAsync(new UpdateNickNameDto
        {
            NickName = "Tom"
        });
        
        result.Nickname.ShouldBe("Tom");
    }
}
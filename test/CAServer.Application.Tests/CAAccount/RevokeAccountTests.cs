using System;
using System.IO;
using System.Threading.Tasks;
using CAServer.amazon;
using CAServer.AppleAuth.Provider;
using CAServer.CAAccount.Dtos;
using CAServer.Commons;
using CAServer.Grain.Tests;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.Guardian;
using CAServer.Options;
using CAServer.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.CAAccount;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class RevokeAccountTests : CAServerApplicationTestBase
{
    private readonly ICAAccountAppService _caAccountAppService;
    private readonly AppleCacheOptions _appleCacheOptions;
    private readonly TestCluster _cluster;
    private ICurrentUser _currentUser;


    public RevokeAccountTests()
    {
        _caAccountAppService = GetRequiredService<ICAAccountAppService>();
        _appleCacheOptions = MockAppleCacheOptions().Value;
        _cluster = GetRequiredService<ClusterFixture>().Cluster;
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockAppleUserProvider());
        services.AddSingleton(GetMockUserAssetsProvider());
        services.AddSingleton(GetMockGuardianProvider());
        services.AddSingleton(GetMockCaAccountProvider());
        services.AddSingleton(GetMockAppleAuthProvider());
        services.AddSingleton(GetMockContractProvider());
        services.AddSingleton(GetMockManagerCountLimitOptions());
    }


    [Fact]
    public async Task Revoke_Test()
    {
        try
        {
            await MockGuardianData();

            await MockCAHolderData();

            await _caAccountAppService.RevokeAsync(new RevokeDto
            {
                AppleToken = "aaaa"
            });
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(ResponseMessage.AppleIdVerifyFail);
        }
    }

    private async Task MockGuardianData()
    {
        var contactGrain = _cluster.Client.GetGrain<IGuardianGrain>("Guardian-MockIdentifier");
        await contactGrain.AddGuardianAsync("aaa", "sss", "xxx", "aaas");
    }

    private async Task MockCAHolderData()
    {
        var contactGrain = _cluster.Client.GetGrain<ICAHolderGrain>(_currentUser.GetId());
        await contactGrain.AddHolderAsync(new CAHolderGrainDto()
        {
            CaHash = "aaa"
        });
    }

    private IOptionsSnapshot<AppleCacheOptions> MockAppleCacheOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<AppleCacheOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new AppleCacheOptions
            {
            });
        return mockOptionsSnapshot.Object;
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

    [Fact]
    public async Task Revoke_Validate_Test()
    {
        var userId = Guid.NewGuid();
        var result = await _caAccountAppService.RevokeValidateAsync(userId, "Email");
        result.ValidatedAssets.ShouldBeTrue();
        result.ValidatedDevice.ShouldBeTrue();
        result.ValidatedGuardian.ShouldBeTrue();
    }

    [Fact]
    public async Task Revoke_Account_Test()
    {
        var input = new RevokeAccountInput
        {
            Token = "MockToken",
            ChainId = "AELF",
            GuardianIdentifier = "MockGuardianIdentifier",
            VerifierId = "",
            Type = "Email",
            VerifierSessionId = Guid.NewGuid()
        };
        var resultDto = await _caAccountAppService.RevokeAccountAsync(input);
        resultDto.Success.ShouldBe(false);
    }


    [Fact]
    public async Task Revoke_Account_GuardianNotExsits_Test()
    {
        var input = new RevokeAccountInput
        {
            Token = "MockToken",
            ChainId = "AELF",
            GuardianIdentifier = "MockGuardianIdentifier",
            VerifierId = "",
            Type = "Google",
            VerifierSessionId = Guid.NewGuid()
        };

        try
        {
            await _caAccountAppService.RevokeAccountAsync(input);
        }
        catch (Exception e)
        {
            e.Message.ShouldContain("Login guardian not exists");
        }
    }


    [Fact]
    public async Task CheckManagerCountAsync_Test()
    {
        var caHash = "MockCaHash";
        var resultDto = await _caAccountAppService.CheckManagerCountAsync(caHash);
        resultDto.ManagersTooMany.ShouldBeTrue();
    }
}
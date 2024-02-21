using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Guardian;
using CAServer.PrivacyPermission.Dtos;
using Shouldly;
using Xunit;
using GuardianDto = CAServer.Guardian.Provider.GuardianDto;

namespace CAServer.PrivacyPermission;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class PrivacyPermissionTest
{
    private readonly IPrivacyPermissionAppService _privacyPermissionAppService;

    [Fact]
    public async Task DeletePrivacyPermissionAsync_test()
    {
        await _privacyPermissionAppService.DeletePrivacyPermissionAsync(null, "caHash", "identifierHash");

        await _privacyPermissionAppService.DeletePrivacyPermissionAsync("chainId", "caHash", "identifierHash");

        await _privacyPermissionAppService.DeletePrivacyPermissionAsync("chainId", "caHash", "identifierHashExist");

        await _privacyPermissionAppService.DeletePrivacyPermissionAsync("chainId", "caHashExist",
            "identifierHashExist");
    }

    [Fact]
    public async Task GetPrivacyPermissionAsync_test()
    {
        var res = new PrivacyPermissionDto();
        res = await _privacyPermissionAppService.GetPrivacyPermissionAsync(Guid.NewGuid());
        res.Id.ShouldBe(Guid.Empty);
        Login(UserId);
        res = await _privacyPermissionAppService.GetPrivacyPermissionAsync(UserIdNullOriginChainId);
        res.Id.ShouldBe(Guid.Empty);

        res = await _privacyPermissionAppService.GetPrivacyPermissionAsync(UserIdGardianNull);
        res.Id.ShouldBe(Guid.Empty);

        res = await _privacyPermissionAppService.GetPrivacyPermissionAsync(UserIdLoginGardianNull);
        res.Id.ShouldBe(Guid.Empty);
        res = await _privacyPermissionAppService.GetPrivacyPermissionAsync(Guid.Empty);
        res.Id.ShouldBe(UserId);
    }

    [Fact]
    public async Task SetPrivacyPermissionAsync_test()
    {
        Login(UserId);
        await _privacyPermissionAppService.SetPrivacyPermissionAsync(new SetPrivacyPermissionInput()
        {
            Identifier = "aaa@bbb"
        });
    }

    [Fact]
    public async Task CheckPrivacyPermissionAsync_test()
    {
        Login(UserId);
        var res = await _privacyPermissionAppService.CheckPrivacyPermissionAsync(new List<Guid>() { Guid.NewGuid() },
            "aaa@bbb", PrivacyType.Email);
        res.Item1.Count.ShouldBe(1);
        res.Item2.Count.ShouldBe(0);

        res = await _privacyPermissionAppService.CheckPrivacyPermissionAsync(new List<Guid>() { UserId }, "aaa@bbb",
            PrivacyType.Email);
        res.Item1.Count.ShouldBe(0);
        res.Item2.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetPrivacyPermissionSettingByGuardiansAsync_test()
    {
        var res =
            await _privacyPermissionAppService
                .GetPrivacyPermissionSettingByGuardiansAsync(new List<GuardianIndexDto>());
        res.Count.ShouldBe(0);

        res = await _privacyPermissionAppService.GetPrivacyPermissionSettingByGuardiansAsync(
            new List<GuardianIndexDto>()
            {
                new GuardianIndexDto()
                {
                    Identifier = "+13822212122"
                },
                new GuardianIndexDto()
                {
                    Identifier = "xxx@aa.com"
                },
                new GuardianIndexDto()
                {
                    Identifier = GoogleIdentifier,
                },
                new GuardianIndexDto()
                {
                    Identifier = AppleIdentifier
                },
                new GuardianIndexDto()
                {
                    Identifier = "xasadasdsad"
                }
            });
        res[PrivacyType.Phone].Count.ShouldBe(1);
        res[PrivacyType.Email].Count.ShouldBe(1);
        res[PrivacyType.Google].Count.ShouldBe(0);
        res[PrivacyType.Apple].Count.ShouldBe(1);
    }

    [Fact]
    public async Task CheckPrivacyPermissionByIdAsync_test()
    {
        Login(UserId);
        var res = await _privacyPermissionAppService.CheckPrivacyPermissionByIdAsync(new List<PermissionSetting>(){new PermissionSetting()
        {
            Identifier = "xxxx@aaa",
            PrivacyType = PrivacyType.Email,
            Permission = PrivacySetting.EveryBody
        }},Guid.NewGuid());
        res.Count.ShouldBe(1);
    }
}
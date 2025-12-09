using CAServer.Grains.Grain.PrivacyPermission;
using CAServer.PrivacyPermission;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.PrivacyPermission;

public class PrivacyPermissionGrainTest : CAServerGrainTestBase
{
    [Fact]
    public async Task GetPrivacyPermissionAsync_test()
    {
        var privacyPermissionGrain = Cluster.Client.GetGrain<IPrivacyPermissionGrain>(Guid.NewGuid());
        await privacyPermissionGrain.GetPrivacyPermissionAsync();
    }
    
    [Fact]
    public async Task SetPermissionAsync_test()
    {
        var identifierPhone = "+13811123123";
        var identifierEmail = "aaa@nnbb";
        var identifierGoogle = "Google";
        var identifierApple = "Apple";
        
        var privacyPermissionGrain = Cluster.Client.GetGrain<IPrivacyPermissionGrain>(Guid.NewGuid());
        await privacyPermissionGrain.SetPermissionAsync(new PermissionSetting
        {
            Identifier = identifierPhone,
            PrivacyType = PrivacyType.Phone,
            Permission = PrivacySetting.EveryBody
        });
        await privacyPermissionGrain.SetPermissionAsync(new PermissionSetting
        {
            Identifier = identifierEmail,
            PrivacyType = PrivacyType.Email,
            Permission = PrivacySetting.EveryBody
        });
        await privacyPermissionGrain.SetPermissionAsync(new PermissionSetting
        {
            Identifier = identifierGoogle,
            PrivacyType = PrivacyType.Google,
            Permission = PrivacySetting.EveryBody
        });
        await privacyPermissionGrain.SetPermissionAsync(new PermissionSetting
        {
            Identifier = identifierApple,
            PrivacyType = PrivacyType.Apple,
            Permission = PrivacySetting.EveryBody
        });
        var res = await privacyPermissionGrain.GetPrivacyPermissionAsync();
        res.AppleList.Count.ShouldBe(1);
        res.EmailList.Count.ShouldBe(1);
        res.GoogleList.Count.ShouldBe(1);
        res.PhoneList.Count.ShouldBe(1);

        await privacyPermissionGrain.DeletePermissionAsync(identifierPhone, PrivacyType.Phone);
        await privacyPermissionGrain.DeletePermissionAsync(identifierEmail, PrivacyType.Email);
        await privacyPermissionGrain.DeletePermissionAsync(identifierGoogle, PrivacyType.Google);
        await privacyPermissionGrain.DeletePermissionAsync(identifierApple, PrivacyType.Apple);
        res = await privacyPermissionGrain.GetPrivacyPermissionAsync();
        res.AppleList.Count.ShouldBe(0);
        res.EmailList.Count.ShouldBe(0);
        res.GoogleList.Count.ShouldBe(0);
        res.PhoneList.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetPermissionAsync_test()
    {
        var identifierPhone = "+13811123123";
        var identifierEmail = "aaa@nnbb";
        var identifierGoogle = "Google";
        var identifierApple = "Apple";
        
        var privacyPermissionGrain = Cluster.Client.GetGrain<IPrivacyPermissionGrain>(Guid.NewGuid());
        await privacyPermissionGrain.SetPermissionAsync(new PermissionSetting
        {
            Identifier = identifierPhone,
            PrivacyType = PrivacyType.Phone,
            Permission = PrivacySetting.EveryBody
        });
        await privacyPermissionGrain.SetPermissionAsync(new PermissionSetting
        {
            Identifier = identifierEmail,
            PrivacyType = PrivacyType.Email,
            Permission = PrivacySetting.EveryBody
        });
        await privacyPermissionGrain.SetPermissionAsync(new PermissionSetting
        {
            Identifier = identifierGoogle,
            PrivacyType = PrivacyType.Google,
            Permission = PrivacySetting.EveryBody
        });
        await privacyPermissionGrain.SetPermissionAsync(new PermissionSetting
        {
            Identifier = identifierApple,
            PrivacyType = PrivacyType.Apple,
            Permission = PrivacySetting.EveryBody
        });

        var res = await privacyPermissionGrain.GetPermissionAsync(null, PrivacyType.Phone);
        res.Count.ShouldBe(0);
        
        res = await privacyPermissionGrain.GetPermissionAsync(new List<PermissionSetting>
        {
            new()
            {
                Identifier = identifierPhone,
                PrivacyType = PrivacyType.Phone,
                Permission = PrivacySetting.EveryBody
            }
        }, PrivacyType.Phone);
        res.Count.ShouldBe(1);
        
        res = await privacyPermissionGrain.GetPermissionAsync(new List<PermissionSetting>
        {
            new()
            {
                Identifier = identifierEmail,
                PrivacyType = PrivacyType.Email,
                Permission = PrivacySetting.EveryBody
            }
        }, PrivacyType.Email);
        res.Count.ShouldBe(1);
        
        res = await privacyPermissionGrain.GetPermissionAsync(new List<PermissionSetting>
        {
            new()
            {
                Identifier = identifierGoogle,
                PrivacyType = PrivacyType.Google,
                Permission = PrivacySetting.EveryBody
            }
        }, PrivacyType.Google);
        res.Count.ShouldBe(1);
        
        res = await privacyPermissionGrain.GetPermissionAsync(new List<PermissionSetting>
        {
            new()
            {
                Identifier = identifierApple,
                PrivacyType = PrivacyType.Apple,
                Permission = PrivacySetting.EveryBody
            }
        }, PrivacyType.Apple);
        res.Count.ShouldBe(1);
    }
}
using CAServer.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace CAServer.Permissions;

public class CAServerPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(CAServerPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(CAServerPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<CAServerResource>(name);
    }
}

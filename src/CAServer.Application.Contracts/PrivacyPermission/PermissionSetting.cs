using System;
using System.Collections.Generic;

namespace CAServer.PrivacyPermission;

public class PermissionSetting
{
    public Guid Id { get; set; }
    public string Identifier { get; set; }
    public PrivacyType PrivacyType { get; set; }
    public PrivacySetting Permission { get; set; }
}

public class PermissionSettingComparer : IEqualityComparer<PermissionSetting>
{
    public bool Equals(PermissionSetting x, PermissionSetting y)
    {
        if (x == null || y == null)
        {
            return false;
        }

        return x.Identifier == y.Identifier;
    }

    public int GetHashCode(PermissionSetting obj)
    {
        return obj.Identifier.GetHashCode();
    }
}
using System;
using System.Collections.Generic;
using Orleans;

namespace CAServer.PrivacyPermission;

[GenerateSerializer]
public class PermissionSetting
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string Identifier { get; set; }
    [Id(2)]
    public PrivacyType PrivacyType { get; set; }
    [Id(3)]
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
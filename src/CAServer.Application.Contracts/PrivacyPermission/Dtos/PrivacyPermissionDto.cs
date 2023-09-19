using System;
using System.Collections.Generic;

namespace CAServer.PrivacyPermission.Dtos;

public class PrivacyPermissionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<PermissionSetting> EmailList { get; set; } = new();
    public List<PermissionSetting> PhoneList { get; set; } = new();
    public List<PermissionSetting> AppleList { get; set; } = new();
    public List<PermissionSetting> GoogleList { get; set; } = new();
}
using System;
using System.Collections.Generic;
using Orleans;

namespace CAServer.PrivacyPermission.Dtos;

[GenerateSerializer]
public class PrivacyPermissionDto
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Guid UserId { get; set; }
    [Id(2)] public List<PermissionSetting> EmailList { get; set; } = new();
    [Id(3)] public List<PermissionSetting> PhoneList { get; set; } = new();
    [Id(4)] public List<PermissionSetting> AppleList { get; set; } = new();
    [Id(5)] public List<PermissionSetting> GoogleList { get; set; } = new();
}
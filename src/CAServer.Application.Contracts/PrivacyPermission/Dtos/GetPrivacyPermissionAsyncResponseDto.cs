using System;
using System.Collections.Generic;

namespace CAServer.PrivacyPermission.Dtos;

public class GetPrivacyPermissionAsyncResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<PermissionSetting> Permissions { get; set; } = new();
}
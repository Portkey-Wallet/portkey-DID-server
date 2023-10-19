using CAServer.PrivacyPermission;

namespace CAServer.Grains.State.PrivacyPermission;

public class PrivacyPermissionState
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<PermissionSetting> EmailList { get; set; } = new();
    public List<PermissionSetting> PhoneList { get; set; } = new();
    public List<PermissionSetting> AppleList { get; set; } = new();
    public List<PermissionSetting> GoogleList { get; set; } = new();
}
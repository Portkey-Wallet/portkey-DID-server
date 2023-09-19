using CAServer.PrivacyPermission;
using CAServer.PrivacyPermission.Dtos;
using Orleans;

namespace CAServer.Grains.Grain.PrivacyPermission;

public interface IPrivacyPermissionGrain : IGrainWithGuidKey
{
    Task<List<PermissionSetting>> GetPermissionAsync(List<PermissionSetting> checkList, PrivacyType privacyType);
    Task SetPermissionAsync(PermissionSetting setting);
    Task<PrivacyPermissionDto> GetPrivacyPermissionAsync();
    Task<bool> IsPermissionAllowAsync(string identifier, PrivacyType type, bool isContract);
}
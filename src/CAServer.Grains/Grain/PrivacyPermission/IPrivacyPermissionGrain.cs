using CAServer.PrivacyPermission;
using CAServer.PrivacyPermission.Dtos;

namespace CAServer.Grains.Grain.PrivacyPermission;

public interface IPrivacyPermissionGrain : IGrainWithGuidKey
{
    Task<int> DeletePermissionAsync(string identifier, PrivacyType type);
    Task<List<PermissionSetting>> GetPermissionAsync(List<PermissionSetting> checkList, PrivacyType privacyType);
    Task SetPermissionAsync(PermissionSetting setting);
    Task<PrivacyPermissionDto> GetPrivacyPermissionAsync();
    //Task<(bool,string)> IsPermissionAllowAsync(string identifier, PrivacyType type, bool isContract);
}
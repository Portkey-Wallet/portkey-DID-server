using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Guardian;
using CAServer.PrivacyPermission.Dtos;

namespace CAServer.PrivacyPermission;

public interface IPrivacyPermissionAppService
{
    Task<PrivacyPermissionDto> GetPrivacyPermissionAsync();
    Task SetPrivacyPermissionAsync(SetPrivacyPermissionInput input);
    Task<(List<Guid>, List<Guid>)> CheckPrivacyPermissionAsync(List<Guid> userIds, string searchKey, PrivacyType type);

    Task<Dictionary<PrivacyType, List<PermissionSetting>>> GetPrivacyPermissionSettingByGuardiansAsync(
        List<GuardianIndexDto> loginGuardians);
    
}
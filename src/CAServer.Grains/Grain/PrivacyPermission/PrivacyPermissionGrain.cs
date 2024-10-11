using CAServer.Grains.State.PrivacyPermission;
using CAServer.PrivacyPermission;
using CAServer.PrivacyPermission.Dtos;
using MongoDB.Driver.Linq;
using Volo.Abp.ObjectMapping;
using Volo.Abp.PermissionManagement;

namespace CAServer.Grains.Grain.PrivacyPermission;

public class PrivacyPermissionGrain : Orleans.Grain<PrivacyPermissionState>, IPrivacyPermissionGrain
{
    private readonly IObjectMapper _objectMapper;

    public PrivacyPermissionGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, token);
    }

    public async Task<PrivacyPermissionDto> GetPrivacyPermissionAsync()
    {
        return _objectMapper.Map<PrivacyPermissionState, PrivacyPermissionDto>(State);
    }

    public async Task<int> DeletePermissionAsync(string identifier, PrivacyType type)
    {
        var checkList = State.PhoneList;
        switch (type)
        {
            case PrivacyType.Phone:
                checkList = State.PhoneList;
                break;
            case PrivacyType.Email:
                checkList = State.EmailList;
                break;
            case PrivacyType.Google:
                checkList = State.GoogleList;
                break;
            case PrivacyType.Apple:
                checkList = State.AppleList;
                break;
        }

        var count = checkList.RemoveAll(item => item.Identifier == identifier);
        await WriteStateAsync();
        return count;
    }
    
/*
    public async Task<(bool, string)> IsPermissionAllowAsync(string identifier, PrivacyType type, bool isContract)
    {
        var checkList = new List<PermissionSetting>();
        switch (type)
        {
            case PrivacyType.Phone:
                checkList = State.PhoneList;
                break;
            case PrivacyType.Email:
                checkList = State.EmailList;
                break;
            case PrivacyType.Google:
                checkList = State.GoogleList;
                break;
            case PrivacyType.Apple:
                checkList = State.AppleList;
                break;
        }

        var item = checkList.FirstOrDefault(dto => dto.Identifier == identifier);
        if (item == null || item.Permission == PrivacySetting.MyContacts)
        {
            if (isContract)
            {
                return (true, PermissionCheckResult.AllowMyContacts);
            }
            return (false, PermissionCheckResult.RejectMyContacts);
        }

        if (item.Permission == PrivacySetting.Nobody)
        {
            return (false, PermissionCheckResult.RejectNobody);
        }

        if (item.Permission == PrivacySetting.EveryBody)
        {
            return (true,PermissionCheckResult.AllowEveryBody);
        }

        return (false,PermissionCheckResult.RejectMyContacts);
    }
    */

    public async Task SetPermissionAsync(PermissionSetting setting)
    {
        switch (setting.PrivacyType)
        {
            case PrivacyType.Email:
                UpdatePermission(State.EmailList, setting);
                break;
            case PrivacyType.Phone:
                UpdatePermission(State.PhoneList, setting);
                break;
            case PrivacyType.Google:
                UpdatePermission(State.GoogleList, setting);
                break;
            case PrivacyType.Apple:
                UpdatePermission(State.AppleList, setting);
                break;
        }

        await WriteStateAsync();
    }

    public async Task<List<PermissionSetting>> GetPermissionAsync(List<PermissionSetting> checkList,
        PrivacyType privacyType)
    {
        if (checkList == null || checkList.Count == 0)
        {
            return new List<PermissionSetting>();
        }

        var commonItems = new List<PermissionSetting>();

        if (privacyType == PrivacyType.Phone)
        {
            commonItems = State.PhoneList.Intersect(checkList, new PermissionSettingComparer()).ToList();
        }
        else if (privacyType == PrivacyType.Email)
        {
            commonItems = State.EmailList.Intersect(checkList, new PermissionSettingComparer()).ToList();
        }
        else if (privacyType == PrivacyType.Apple)
        {
            commonItems = State.AppleList.Intersect(checkList, new PermissionSettingComparer()).ToList();
        }
        else
        {
            commonItems = State.GoogleList.Intersect(checkList, new PermissionSettingComparer()).ToList();
        }

        var remainingItems = checkList.Except(commonItems, new PermissionSettingComparer()).ToList();

        foreach (var permissionSetting in remainingItems)
        {
            permissionSetting.Permission = PrivacySetting.MyContacts;
        }

        var result = new List<PermissionSetting>();
        result.AddRange(commonItems);
        result.AddRange(remainingItems);
        foreach (var permissionSetting in result)
        {
            permissionSetting.PrivacyType = privacyType;
        }

        result = result.Where(x => x.Identifier != null).ToList();

        return result;
    }

    private void UpdatePermission(List<PermissionSetting> list, PermissionSetting setting)
    {
        var item = list.FirstOrDefault(dto => dto.Identifier == setting.Identifier);
        if (item != null)
        {
            item.Permission = setting.Permission;
        }
        else
        {
            list.Add(setting);
        }
    }
}
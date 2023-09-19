using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.PrivacyPermission;
using CAServer.Guardian;
using CAServer.Guardian.Provider;
using CAServer.PrivacyPermission.Dtos;
using CAServer.UserAssets.Provider;
using CAServer.UserExtraInfo;
using CAServer.UserExtraInfo.Dtos;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.Users;

namespace CAServer.PrivacyPermission;

public class PrivacyPermissionAppService : CAServerAppService, IPrivacyPermissionAppService
{
    private readonly IUserAssetsProvider _userAssetsProvider;
    private readonly IGuardianProvider _guardianProvider;
    private readonly IGuardianAppService _guardianAppService;
    private readonly IUserExtraInfoAppService _userExtraInfoAppService;
    private readonly IContactProvider _contactProvider;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<PrivacyPermissionAppService> _logger;
    
    public PrivacyPermissionAppService(IUserAssetsProvider userAssetsProvider, IGuardianProvider guardianProvider,
        IGuardianAppService guardianAppService, IUserExtraInfoAppService userExtraInfoAppService,
        IContactProvider contactProvider, IClusterClient clusterClient, ILogger<PrivacyPermissionAppService> logger)
    {
        _userAssetsProvider = userAssetsProvider;
        _guardianProvider = guardianProvider;
        _guardianAppService = guardianAppService;
        _userExtraInfoAppService = userExtraInfoAppService;
        _contactProvider = contactProvider;
        _clusterClient = clusterClient;
        _logger = logger;
    }
    
    //todo:增加删除事件
    
    public async Task<PrivacyPermissionDto> GetPrivacyPermissionAsync()
    {
        var userId = CurrentUser.GetId();
        var caHolderIndex = await _userAssetsProvider.GetCaHolderIndexAsync(userId);
        if (caHolderIndex == null)
        {
            return new PrivacyPermissionDto();
        }
        var holderInfo = await _guardianProvider.GetGuardiansAsync(null, caHolderIndex.CaHash);
        var originChainId = holderInfo.CaHolderInfo?.FirstOrDefault()?.OriginChainId;
        if (string.IsNullOrWhiteSpace(originChainId))
        {
            return new PrivacyPermissionDto();
        }
        
        var guardianInfo =
            holderInfo.CaHolderInfo.FirstOrDefault(g => g.GuardianList != null && g.GuardianList.Guardians.Count > 0 && g.OriginChainId == originChainId);

        if (guardianInfo == null)
        {
            return new PrivacyPermissionDto();
        }

        var loginGuardians = guardianInfo.GuardianList.Guardians.Where(g => g.IsLoginGuardian).ToList();
        if (loginGuardians.Count == 0)
        {
            return new PrivacyPermissionDto();
        }
        
        var loginGuardianIdentifierHashes = loginGuardians.Select(g => g.IdentifierHash).ToList();
        var guardianListDto = await _guardianAppService.GetGuardianListAsync(loginGuardianIdentifierHashes);
        if (guardianListDto == null || guardianListDto.Count == 0)
        {
            return new PrivacyPermissionDto();
        }
        
        var privacyPermissionMap = await GetPrivacyPermissionSettingByGuardiansAsync(guardianListDto);
        var privacyPermissionGrain = _clusterClient.GetGrain<IPrivacyPermissionGrain>(userId);
        
        var result = new PrivacyPermissionDto();
        result.Id = userId;
        result.UserId = userId;
        result.PhoneList =
            await privacyPermissionGrain.GetPermissionAsync(privacyPermissionMap[PrivacyType.Phone], PrivacyType.Phone);
        result.EmailList =
            await privacyPermissionGrain.GetPermissionAsync(privacyPermissionMap[PrivacyType.Email], PrivacyType.Email);
        result.AppleList =
            await privacyPermissionGrain.GetPermissionAsync(privacyPermissionMap[PrivacyType.Apple], PrivacyType.Apple);
        result.GoogleList =
            await privacyPermissionGrain.GetPermissionAsync(privacyPermissionMap[PrivacyType.Google], PrivacyType.Google);
        return result;
    }
    
    public async Task SetPrivacyPermissionAsync(SetPrivacyPermissionInput input)
    {
        var privacyPermissionGrain = _clusterClient.GetGrain<IPrivacyPermissionGrain>(CurrentUser.GetId());
        await privacyPermissionGrain.SetPermissionAsync(new PermissionSetting()
        {
            Id = Guid.NewGuid(),
            Identifier = input.Identifier,
            PrivacyType = input.PrivacyType,
            Permission = input.Permission
        });
    }

    public async Task<(List<Guid>, List<Guid>)> CheckPrivacyPermissionAsync(List<Guid> userIds, string searchKey,
        PrivacyType type)
    {
        var approvedUserIds = new List<Guid>();
        var rejectedUserIds = new List<Guid>();
        var currentUserId = CurrentUser.GetId();
        
        
        var contactList = await _contactProvider.GetContactsAsync(currentUserId);
        
        var permissionCheckTasks = new List<Task<bool>>();
        foreach (var userId in userIds)
        {
            var grain = _clusterClient.GetGrain<IPrivacyPermissionGrain>(userId);
            var isContract = contactList.Any(contact => contact.UserId == userId);
            permissionCheckTasks.Add(grain.IsPermissionAllowAsync(searchKey, type, isContract));
        }
        
        var results = await Task.WhenAll(permissionCheckTasks);
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i])
            {
                approvedUserIds.Add(userIds[i]);
            }
            else
            {
                rejectedUserIds.Add(userIds[i]);
            }
        }
        return (approvedUserIds,rejectedUserIds);
    }
    
    public async Task<Dictionary<PrivacyType,List<PermissionSetting>>> GetPrivacyPermissionSettingByGuardiansAsync(List<GuardianIndexDto> loginGuardians)
    {
        var guardians = loginGuardians
            .Where(dto => !string.IsNullOrEmpty(dto.Identifier)) 
            .GroupBy(dto => dto.Identifier) 
            .Select(group => group.OrderByDescending(dto => dto.IdentifierHash).First()) 
            .ToList();
        
        if (guardians.Count == 0)
        {
            return new Dictionary<PrivacyType, List<PermissionSetting>>();
        }
        
        var resultDic = new Dictionary<PrivacyType, List<PermissionSetting>>();
        resultDic.Add(PrivacyType.Phone, new List<PermissionSetting>());
        resultDic.Add(PrivacyType.Email, new List<PermissionSetting>());
        resultDic.Add(PrivacyType.Apple, new List<PermissionSetting>());
        resultDic.Add(PrivacyType.Google, new List<PermissionSetting>());
        
        foreach (var guardianIndexDto in guardians)
        {
            if (IsPhone(guardianIndexDto.Identifier))
            {
                resultDic[PrivacyType.Phone].Add(new PermissionSetting()
                {
                    Identifier = guardianIndexDto.Identifier
                });
            }
            else if (IsEmail(guardianIndexDto.Identifier))
            {
                resultDic[PrivacyType.Email].Add(new PermissionSetting()
                {
                    Identifier = guardianIndexDto.Identifier
                });
            }
            else
            {
                var userExtraInfo = new UserExtraInfoResultDto();
                try
                {
                    userExtraInfo = await _userExtraInfoAppService.GetUserExtraInfoAsync(guardianIndexDto.Identifier);
                }
                catch (Exception e)
                {
                    _logger.LogError(e,"get user extra info error, Identifier:{Identifier}", guardianIndexDto.Identifier);
                    continue;
                }
                
                switch (userExtraInfo.GuardianType)
                {
                    case "Apple":
                        resultDic[PrivacyType.Google].Add(new PermissionSetting()
                        {
                            Identifier = guardianIndexDto.Identifier
                        });
                        break;
                    case "Google":
                        resultDic[PrivacyType.Apple].Add(new PermissionSetting()
                        {
                            Identifier = guardianIndexDto.Identifier
                        });
                        break;
                    default:
                        _logger.LogError("get user extra GuardianType error, Identifier:{Identifier},type:{type}",
                            guardianIndexDto.Identifier, userExtraInfo.GuardianType);
                        break;
                }
            }
        }

        return resultDic;
    }
    
    private static bool IsPhone(string input)
    {
        return input.All(char.IsDigit) && input.StartsWith("+");
    }
    
    private static bool IsEmail(string input)
    {
        return input.Count(c => c == '@') == 1;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.Common;
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
    private readonly IContractProvider _contractProvider;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<PrivacyPermissionAppService> _logger;
    
    public PrivacyPermissionAppService(IUserAssetsProvider userAssetsProvider, IGuardianProvider guardianProvider,
        IGuardianAppService guardianAppService, IUserExtraInfoAppService userExtraInfoAppService,
        IContactProvider contactProvider, IContractProvider contractProvider ,IClusterClient clusterClient, ILogger<PrivacyPermissionAppService> logger)
    {
        _userAssetsProvider = userAssetsProvider;
        _guardianProvider = guardianProvider;
        _guardianAppService = guardianAppService;
        _userExtraInfoAppService = userExtraInfoAppService;
        _contactProvider = contactProvider;
        _contractProvider = contractProvider;
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public async Task DeletePrivacyPermissionAsync(string chainId ,string caHash, string identifierHash)
    {
        if (string.IsNullOrWhiteSpace(chainId) || string.IsNullOrWhiteSpace(caHash) || string.IsNullOrWhiteSpace(identifierHash))
        {
            return;
        }
        
        var guardianListDto = await _guardianAppService.GetGuardianListAsync(new List<string>{identifierHash});
        if (guardianListDto == null || guardianListDto.Count == 0)
        {
            return;
        }

        var holder = await _userAssetsProvider.GetCaHolderIndexByCahashAsync(caHash);
        if (holder == null)
        {
            return;
        }
        var privacyPermissionGrain = _clusterClient.GetGrain<IPrivacyPermissionGrain>(holder.UserId);
        var type = await GetPrivacyTypeAsync(guardianListDto.First());
        
        await privacyPermissionGrain.DeletePermissionAsync(guardianListDto.First().Identifier, type);
    }
    
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
            var type = await GetPrivacyTypeAsync(guardianIndexDto);
            if (type == PrivacyType.Unknow)
            {
                continue;
            }

            switch (type)
            {
                case PrivacyType.Phone:
                case PrivacyType.Email:
                    resultDic[type].Add(new PermissionSetting()
                    {
                        Identifier = guardianIndexDto.Identifier
                    });
                    break;
                case PrivacyType.Google:
                case PrivacyType.Apple:
                    var userExtraInfo = await _userExtraInfoAppService.GetUserExtraInfoAsync(guardianIndexDto.Identifier);
                    if (userExtraInfo.VerifiedEmail && userExtraInfo.IsPrivate == false)
                    {
                        resultDic[type].Add(new PermissionSetting()
                        {
                            Identifier = userExtraInfo.Email
                        });
                    }
                    break;
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

    private async Task<PrivacyType> GetPrivacyTypeAsync(GuardianIndexDto guardianIndexDto)
    {
        if (IsPhone(guardianIndexDto.Identifier))
        {
            return PrivacyType.Phone;
        }
        
        if (IsEmail(guardianIndexDto.Identifier))
        {
            return PrivacyType.Email;
        }
        
        var userExtraInfo = new UserExtraInfoResultDto();
        try
        {
            userExtraInfo = await _userExtraInfoAppService.GetUserExtraInfoAsync(guardianIndexDto.Identifier);
        }
        catch (Exception e)
        {
            _logger.LogError(e,"get user extra info error, Identifier:{Identifier}", guardianIndexDto.Identifier);
            return PrivacyType.Unknow;
        }
                
        switch (userExtraInfo.GuardianType)
        {
            case "Apple":
                return PrivacyType.Apple;
            case "Google":
                return PrivacyType.Google;
            default:
                _logger.LogError("get user extra GuardianType error, Identifier:{Identifier},type:{type}",
                    guardianIndexDto.Identifier, userExtraInfo.GuardianType);
                return PrivacyType.Unknow;
        }
        return PrivacyType.Unknow;
    }
}
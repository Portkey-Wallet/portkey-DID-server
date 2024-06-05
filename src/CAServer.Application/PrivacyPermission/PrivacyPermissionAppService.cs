using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Contacts;
using CAServer.Contacts.Provider;
using CAServer.Grains.Grain.PrivacyPermission;
using CAServer.Guardian;
using CAServer.Guardian.Provider;
using CAServer.PrivacyPermission.Dtos;
using CAServer.UserAssets.Provider;
using CAServer.UserExtraInfo;
using CAServer.UserExtraInfo.Dtos;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace CAServer.PrivacyPermission;

[RemoteService(false)]
[DisableAuditing]
public class PrivacyPermissionAppService : CAServerAppService, IPrivacyPermissionAppService
{
    private readonly IUserAssetsProvider _userAssetsProvider;
    private readonly IGuardianProvider _guardianProvider;
    private readonly IGuardianAppService _guardianAppService;
    private readonly IUserExtraInfoAppService _userExtraInfoAppService;
    private readonly IContactProvider _contactProvider;
    private readonly IContractProvider _contractProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IContactAppService _contactAppService;
    private readonly ILogger<PrivacyPermissionAppService> _logger;
    
    public PrivacyPermissionAppService(IUserAssetsProvider userAssetsProvider, IGuardianProvider guardianProvider,
        IGuardianAppService guardianAppService, IUserExtraInfoAppService userExtraInfoAppService,
        IContactProvider contactProvider, IContractProvider contractProvider, IClusterClient clusterClient,
        IContactAppService contactAppService, ILogger<PrivacyPermissionAppService> logger)
    {
        _userAssetsProvider = userAssetsProvider;
        _guardianProvider = guardianProvider;
        _guardianAppService = guardianAppService;
        _userExtraInfoAppService = userExtraInfoAppService;
        _contactProvider = contactProvider;
        _contractProvider = contractProvider;
        _clusterClient = clusterClient;
        _contactAppService = contactAppService;
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
        
        var count = await privacyPermissionGrain.DeletePermissionAsync(guardianListDto.First().Identifier, type);
        _logger.LogInformation(
            "DeletePrivacyPermissionAsync count:{count},cahash:{caHash},identifier:{identifier},type:{type}", count,
            caHash, guardianListDto.First().Identifier, type);
    }
    
    public async Task<PrivacyPermissionDto> GetPrivacyPermissionAsync(Guid id)
    {
        var userId = id;
        if (userId == Guid.Empty)
        {
            userId = CurrentUser.GetId();
        }
        
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
        
        var tasks = new List<Task>();
        tasks.Add(Task.Run(async () =>
        {
            result.PhoneList = await privacyPermissionGrain.GetPermissionAsync(privacyPermissionMap[PrivacyType.Phone], PrivacyType.Phone);
        }));
        tasks.Add(Task.Run(async () =>
        {
            result.EmailList = await privacyPermissionGrain.GetPermissionAsync(privacyPermissionMap[PrivacyType.Email], PrivacyType.Email);
        }));
        tasks.Add(Task.Run(async () =>
        {
            result.AppleList = await privacyPermissionGrain.GetPermissionAsync(privacyPermissionMap[PrivacyType.Apple], PrivacyType.Apple);
        }));
        tasks.Add(Task.Run(async () =>
        {
            result.GoogleList = await privacyPermissionGrain.GetPermissionAsync(privacyPermissionMap[PrivacyType.Google], PrivacyType.Google);
        }));
        
        await Task.WhenAll(tasks);
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
    
    public async Task<List<PermissionSetting>> CheckPrivacyPermissionByIdAsync(List<PermissionSetting> input ,Guid id)
    {
        /*
         * The logic is as follows,
         * If a follows b, and b sets the contact to visible, then no matter whether b follows a, a can see b's loginGurdian
         * This method is whether a has the right to see b's contacts, so the logic is to see if a is in b's friend list
         */
        //var contact1 = await _contactProvider.GetContactAsync(CurrentUser.GetId(), id);
        var contact2 = await _contactProvider.GetContactAsync(id, CurrentUser.GetId());
        var isContact = (contact2 != null);
        var result = input.Where(x => x.Permission != PrivacySetting.Nobody).ToList();
        if (isContact == false)
        {
            result = result.Where(x => x.Permission != PrivacySetting.MyContacts).ToList();
        }

        return result;
    }

    public async Task<(List<Guid>, List<Guid>)> CheckPrivacyPermissionAsync(List<Guid> userIds, string searchKey,
        PrivacyType type)
    {
        var approvedUserIds = new List<Guid>();
        var rejectedUserIds = new List<Guid>();
        var currentUserId = CurrentUser.GetId();

        foreach (var userId in userIds)
        {
            if (userId == currentUserId)
            {
                rejectedUserIds.Add(userId);
                _logger.LogInformation(
                    "CheckPrivacyPermissionAsync current:{currentUserId},CheckUserId:{userId},isAllow:{isAllow},type:{type},searchKey:{searchKey}",
                    currentUserId, userId,
                    true, type, searchKey);
                continue;
            }

            approvedUserIds.Add(userId);
            _logger.LogInformation(
                "CheckPrivacyPermissionAsync current:{currentUserId},CheckUserId:{userId},isAllow:{isAllow},reason:{reason},type:{type},searchKey:{searchKey}",
                currentUserId, userId,
                false, type, searchKey);
        }
        
        /*var permissionCheckTasks = new List<Task<(bool,string)>>();
        foreach (var userId in userIds)
        {
            var grain = _clusterClient.GetGrain<IPrivacyPermissionGrain>(userId);
            var isContract = await _contactAppService.GetExistByUserIdAsync(userId);
            permissionCheckTasks.Add(grain.IsPermissionAllowAsync(searchKey, type, isContract.Existed));
        }
        
        var results = await Task.WhenAll(permissionCheckTasks);
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i].Item1)
            {
                approvedUserIds.Add(userIds[i]);
            }
            else
            {
                rejectedUserIds.Add(userIds[i]);
            }

            _logger.LogInformation(
                "CheckPrivacyPermissionAsync current:{currentUserId},CheckUserId:{userId},isAllow:{isAllow},reason:{reason},type:{type},searchKey:{searchKey}",
                currentUserId, userIds[i],
                results[i].Item1, results[i].Item2, type, searchKey);
        }*/
        
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
                        Identifier = guardianIndexDto.Identifier,
                        PrivacyType = type
                    });
                    break;
                case PrivacyType.Google:
                case PrivacyType.Apple:
                    try
                    {
                        var userExtraInfo = await _userExtraInfoAppService.GetUserExtraInfoAsync(guardianIndexDto.Identifier);
                        if (userExtraInfo.VerifiedEmail && userExtraInfo.IsPrivate == false)
                        {
                            resultDic[type].Add(new PermissionSetting()
                            {
                                Identifier = userExtraInfo.Email,
                                PrivacyType = type
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e,"get user extra info error, Identifier:{Identifier}", guardianIndexDto.Identifier);          
                    }
                    break;
            }
        }

        return resultDic;
    }
    
    private static bool IsPhone(string input)
    {
        var pattern = @"^\+\d+$";
        return Regex.IsMatch(input, pattern);
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
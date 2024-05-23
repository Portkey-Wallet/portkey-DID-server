using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Account;
using CAServer.Common;
using CAServer.Contacts;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.PrivacyPermission;
using CAServer.Guardian;
using CAServer.Guardian.Provider;
using CAServer.PrivacyPermission.Dtos;
using CAServer.UserAssets.Provider;
using CAServer.UserExtraInfo;
using CAServer.UserExtraInfo.Dtos;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
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
    private readonly INESTRepository<UserExtraInfoIndex, string> _userExtraInfoRepository;
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;
    
    public PrivacyPermissionAppService(IUserAssetsProvider userAssetsProvider, IGuardianProvider guardianProvider,
        IGuardianAppService guardianAppService, IUserExtraInfoAppService userExtraInfoAppService,
        IContactProvider contactProvider, IContractProvider contractProvider, IClusterClient clusterClient,
        IContactAppService contactAppService, ILogger<PrivacyPermissionAppService> logger,
        INESTRepository<UserExtraInfoIndex, string> userExtraInfoRepository, INESTRepository<GuardianIndex, string> guardianRepository)
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
        _userExtraInfoRepository = userExtraInfoRepository;
        _guardianRepository = guardianRepository;
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

        try
        {
            _logger.LogInformation("DeletePrivacyPermissionAsync is running chainId={0}, caHash={1},  identifierHash={2}", chainId , caHash, identifierHash);
            var grain = _clusterClient.GetGrain<ICAHolderGrain>(holder.UserId);
            var caHolderGrainDto = grain.GetCaHolder();
            _logger.LogInformation("caHolderGrainDto from grain ={0}", JsonConvert.SerializeObject(caHolderGrainDto));
            if (caHolderGrainDto == null || caHolderGrainDto.Result == null || caHolderGrainDto.Result.Data == null)
            {
                _logger.LogError("query caHolderGrainDto from ICAHolderGrain is null, caHash={0}", caHash);
                return;
            }
            var caHolderFromGrain = caHolderGrainDto.Result.Data;
            //condition: not use login account strategy, pass
            if (caHolderFromGrain.IdentifierHash.IsNullOrEmpty() || !caHolderFromGrain.ModifiedNickname)
            {
                return;
            }
            //condition: the identifierHash is not the deleted one, pass
            if (!caHolderFromGrain.IdentifierHash.Equals(identifierHash))
            {
                return;
            }
            var holderInfo = await _guardianProvider.GetGuardiansAsync(null, caHash);
            _logger.LogInformation("holderInfo ={0}", JsonConvert.SerializeObject(holderInfo));
            var guardianInfo = holderInfo.CaHolderInfo.FirstOrDefault(g => g.GuardianList != null
                                                                           && g.GuardianList.Guardians.Count > 0);
            string nickname = caHolderFromGrain.UserId.ToString("N").Substring(0, 8);
            _logger.LogInformation("guardianInfo ={0}", JsonConvert.SerializeObject(guardianInfo));
            if (guardianInfo == null)
            {
                await DealWithThirdParty(nickname, chainId, caHash, holder.UserId, identifierHash);
                return;
            }
            var guardianInfoBase = guardianInfo.GuardianList.Guardians.FirstOrDefault(g => g.IsLoginGuardian);
            if (guardianInfoBase == null || !guardianInfoBase.Type.Equals(((int)GuardianType.GUARDIAN_TYPE_OF_EMAIL) + ""))
            {
                await DealWithThirdParty(nickname, chainId, caHash, holder.UserId, identifierHash);
                return;
            }
            
            await DealWithEmail(nickname,  caHash,  identifierHash, holder.UserId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "exception occured when modify the nickname, chainId={0}, caHash={1}, identifierHash={2}",chainId, caHash, identifierHash);
        }
    }
    
    private string GetFirstNameFormat(string nickname, string firstName, string address)
    {
        if (firstName.IsNullOrEmpty() && address.IsNullOrEmpty())
        {
            return nickname;
        }
        if (!firstName.IsNullOrEmpty() && Regex.IsMatch(firstName,"^\\w+$"))
        {
            return firstName + "***";
        }

        if (!address.IsNullOrEmpty())
        {
            int length = address.Length;
            return address.Substring(0, 3) + "***" + address.Substring(length - 3);
        }
        return nickname;
    }

    private async Task DealWithThirdParty(string nickname, string chainId, string caHash, Guid userId, string identifierHash)
    {
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        GuardianIdentifierDto guardianIdentifierDto = new GuardianIdentifierDto();
        guardianIdentifierDto.ChainId = chainId;
        guardianIdentifierDto.CaHash = caHash;
        var guardianResultDto = await _guardianAppService.GetGuardianIdentifiersAsync(guardianIdentifierDto);
        _logger.LogInformation("third party guardianResultDto ={0}", JsonConvert.SerializeObject(guardianResultDto));
        var guardian = guardianResultDto.GuardianList.Guardians.FirstOrDefault(g => g.IsLoginGuardian && !g.ThirdPartyEmail.IsNullOrEmpty());
        _logger.LogInformation("third party guardian ={0}", JsonConvert.SerializeObject(guardian));
        string changedNickname = string.Empty;
        string address = string.Empty;
        if (guardianResultDto.ManagerInfos != null)
        {
            var managerInfoDto = guardianResultDto.ManagerInfos.FirstOrDefault(m => !m.Address.IsNullOrEmpty());
            if (managerInfoDto != null)
            {
                address = managerInfoDto.Address;
            }
        }
        GrainResultDto<CAHolderGrainDto> result = null;
        if (guardian == null)
        {
            result = await grain.UpdateNicknameAndMarkBitAsync(nickname, false, string.Empty);
        }
        else if (guardian.ThirdPartyEmail.IsNullOrEmpty())
        {
            if ("Telegram".Equals(guardian.Type) || "Twitter".Equals(guardian.Type))
            {
                changedNickname = GetFirstNameFormat(nickname, guardian.FirstName, address);
            }
            if ("Email".Equals(guardian.Type) && !guardian.GuardianIdentifier.IsNullOrEmpty())
            {
                changedNickname = GetEmailFormat(nickname, guardian.GuardianIdentifier);
            }
            if (string.Empty.Equals(changedNickname) || nickname.Equals(changedNickname))
            {
                result = await grain.UpdateNicknameAndMarkBitAsync(nickname, false, string.Empty);
            }
            else
            {
                result = await grain.UpdateNicknameAndMarkBitAsync(changedNickname, true, identifierHash);
            }
        }
        else
        {
            changedNickname = GetEmailFormat(nickname, guardian.ThirdPartyEmail);
            if (string.Empty.Equals(changedNickname) || nickname.Equals(changedNickname))
            {
                result = await grain.UpdateNicknameAndMarkBitAsync(nickname, false, string.Empty);
            }
            else
            {
                result = await grain.UpdateNicknameAndMarkBitAsync(changedNickname, true, identifierHash);
            }
        }
        if (result != null && !result.Success)
        {
            _logger.LogError("update third party user nick name failed, nickname={0}, changedNickname={1}", nickname, changedNickname);
        }
    }

    private async Task DealWithEmail(string nickname, string caHash, string identifierHash, Guid userId)
    {
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        GuardianInfoBase guardianInfoBase = await GetLoginAccountInfo(caHash, identifierHash);
        _logger.LogInformation("guardianInfoBase ={0}", JsonConvert.SerializeObject(guardianInfoBase));
        _logger.LogInformation("nickname ={0}", nickname);
        string changedNickname = GenerateNewAccountFormat(nickname, guardianInfoBase);
        _logger.LogInformation("changedNickname ={0}", changedNickname);
        GrainResultDto<CAHolderGrainDto> result = null;
        if (nickname.Equals(changedNickname))
        {
            result = await grain.UpdateNicknameAndMarkBitAsync(nickname, false, string.Empty);
        }
        else
        {
            result = await grain.UpdateNicknameAndMarkBitAsync(changedNickname, true, guardianInfoBase.IdentifierHash);
        }
        if (result != null && !result.Success)
        {
            _logger.LogError("update email user nick name failed, nickname={0}, changedNickname={1}", nickname, changedNickname);
        }
    }
    
    private string GenerateNewAccountFormat(string nickname, GuardianInfoBase guardianInfoBase)
    {
        if (guardianInfoBase == null)
        {
            _logger.LogInformation("nickname={0} guardianInfoBase is null", nickname);
            return nickname;
        }

        if (!guardianInfoBase.IsLoginGuardian)
        {
            _logger.LogInformation("nickname={0} guardianInfoBase is not login guardian", nickname);
            return nickname;
        }

        string guardianIdentifier = guardianInfoBase.GuardianIdentifier;
        string guardianType = guardianInfoBase.Type;
        if (guardianIdentifier == null)
        {
            _logger.LogInformation("nickname={0} guardianIdentifier is null", nickname);
            return nickname;
        }
        //email  according to GuardianType
        if ((int)GuardianType.GUARDIAN_TYPE_OF_EMAIL == int.Parse(guardianType))
        {
            if (!guardianIdentifier.Contains("@"))
            {
                _logger.LogInformation("nickname={0} guardianInfoBase is not login guardian", nickname);
                return nickname;
            }
            return GetEmailFormat(nickname, guardianIdentifier);
        }
        return nickname;
    }
    
    private async Task<List<UserExtraInfoIndex>> GetUserExtraInfoAsync(List<string> identifiers)
    {
        try
        {
            if (identifiers == null || identifiers.Count == 0)
            {
                return new List<UserExtraInfoIndex>();
            }

            var mustQuery = new List<Func<QueryContainerDescriptor<UserExtraInfoIndex>, QueryContainer>>
            {
                q => q.Terms(i => i.Field(f => f.Id).Terms(identifiers))
            };

            QueryContainer Filter(QueryContainerDescriptor<UserExtraInfoIndex> f) =>
                f.Bool(b => b.Must(mustQuery));

            var userExtraInfos = await _userExtraInfoRepository.GetListAsync(Filter);

            return userExtraInfos.Item2;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "in GetUserExtraInfoAsync");
        }

        return new List<UserExtraInfoIndex>();
    }

    private string GetEmailFormat(string nickname, string guardianIdentifier)
    {
        int index = guardianIdentifier.LastIndexOf("@");
        if (index < 0)
        {
            return nickname;
        }
        string frontPart = guardianIdentifier.Substring(0, index);
        string backPart = guardianIdentifier.Substring(index);
        int frontLength = frontPart.Length;
        if (frontLength > 4)
        {
            return frontPart.Substring(0, 4) + "***" + backPart;
        }
        else
        {
            return frontPart + "***" + backPart;
        }
    }
    
    private async Task<GuardianInfoBase> GetLoginAccountInfo(string caHash, string identifierHash)
    {
        //if the guardian type is third party, the guardianIdentifier of GuardianInfoBase
        var holderInfo = await _guardianProvider.GetGuardiansAsync(null, caHash);
        var guardianInfo = holderInfo.CaHolderInfo.FirstOrDefault(g => g.GuardianList != null
                                                                       && g.GuardianList.Guardians.Count > 0);
        if (guardianInfo == null)
        {
            return null;
        }
        GuardianInfoBase guardianInfoBase = guardianInfo?.GuardianList.Guardians.FirstOrDefault(g => !g.IdentifierHash.Equals(identifierHash));
        if (guardianInfoBase == null)
        {
            return null;
        }
        var list = new List<string>();
        list.Add(guardianInfoBase.IdentifierHash);
        var hashDic = await GetIdentifiersAsync(list);
        guardianInfoBase.GuardianIdentifier = hashDic[guardianInfoBase.IdentifierHash];
        return guardianInfoBase;
    }
    
    private async Task<Dictionary<string, string>> GetIdentifiersAsync(List<string> identifierHashList)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>
        {
            q => q.Terms(i => i.Field(f => f.IdentifierHash).Terms(identifierHashList))
        };

        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var guardians = await _guardianRepository.GetListAsync(Filter);

        var result = guardians.Item2.Where(t => t.IsDeleted == false);

        return result.ToDictionary(t => t.IdentifierHash, t => t.Identifier);
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
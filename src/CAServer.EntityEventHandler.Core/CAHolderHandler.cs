using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains.Grain.Contacts;
using CAServer.Guardian;
using CAServer.Guardian.Provider;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using GuardianInfoBase = CAServer.Guardian.GuardianInfoBase;
using GuardianType = CAServer.Account.GuardianType;
using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;

namespace CAServer.EntityEventHandler.Core;

public class CAHolderHandler : IDistributedEventHandler<CreateUserEto>,
    IDistributedEventHandler<UpdateCAHolderEto>,
    IDistributedEventHandler<DeleteCAHolderEto>
    , ITransientDependency
{
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CAHolderHandler> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IUserTokenAppService _userTokenAppService;
    private readonly IContactProvider _contactProvider;
    private readonly INESTRepository<ContactIndex, Guid> _contactRepository;
    private readonly IGuardianProvider _guardianProvider;
    private readonly INESTRepository<UserExtraInfoIndex, string> _userExtraInfoRepository;
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;
    private readonly IGuardianAppService _guardianAppService;

    public CAHolderHandler(INESTRepository<CAHolderIndex, Guid> caHolderRepository,
        IObjectMapper objectMapper,
        ILogger<CAHolderHandler> logger,
        IClusterClient clusterClient,
        IUserTokenAppService userTokenAppService,
        IContactProvider contactProvider,
        INESTRepository<ContactIndex, Guid> contactRepository,
        IGuardianProvider guardianProvider,
        INESTRepository<UserExtraInfoIndex, string> userExtraInfoRepository,
        INESTRepository<GuardianIndex, string> guardianRepository,
        IGuardianAppService guardianAppService)
    {
        _caHolderRepository = caHolderRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _clusterClient = clusterClient;
        _userTokenAppService = userTokenAppService;
        _contactProvider = contactProvider;
        _contactRepository = contactRepository;
        _guardianProvider = guardianProvider;
        _userExtraInfoRepository = userExtraInfoRepository;
        _guardianRepository = guardianRepository;
        _guardianAppService = guardianAppService;
    }

    public async Task HandleEventAsync(CreateUserEto eventData)
    {
        string changedNickname = string.Empty;
        string nickname = eventData.UserId.ToString("N").Substring(0, 8);
        GuardianInfoBase loginGuardianInfoBase = null;
        try
        {
            loginGuardianInfoBase = await GetLoginAccountInfo(eventData.CaHash);
            _logger.LogInformation("received create user event {0}", JsonConvert.SerializeObject(loginGuardianInfoBase));
            if (loginGuardianInfoBase == null)
            {
                _logger.LogInformation("third party account, eventData={0}", JsonConvert.SerializeObject(eventData));
                changedNickname = await GenerateNewAccountFormatForThirdParty(nickname, eventData);
            }
            else
            {
                _logger.LogInformation("email account, eventData={0}", JsonConvert.SerializeObject(eventData));
                changedNickname = await GenerateNewAccountFormat(nickname, loginGuardianInfoBase);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GenerateNewAccountFormat error, userId={0}, caHash={1}", eventData.UserId, eventData.CaHash);
        }
        try
        {
            var grain = _clusterClient.GetGrain<ICAHolderGrain>(eventData.UserId);
            var caHolderGrainDto = _objectMapper.Map<CreateUserEto, CAHolderGrainDto>(eventData);
            if (changedNickname.IsNullOrEmpty() || nickname.Equals(changedNickname))
            {
                caHolderGrainDto.Nickname = nickname;
                caHolderGrainDto.PopedUp = false;
                caHolderGrainDto.ModifiedNickname = false;
            }
            else
            {
                caHolderGrainDto.Nickname = changedNickname;
                caHolderGrainDto.PopedUp = true;
                caHolderGrainDto.ModifiedNickname = true;
            }
            if (loginGuardianInfoBase != null)
            {
                caHolderGrainDto.IdentifierHash = loginGuardianInfoBase.IdentifierHash;
            }
            var result = await grain.AddHolderAsync(caHolderGrainDto);

            if (!result.Success)
            {
                _logger.LogError("create holder fail: {message}, userId: {userId}, aAHash: {caHash}", result.Message,
                    eventData.UserId, eventData.CaHash);
                return;
            }

            await _caHolderRepository.AddAsync(_objectMapper.Map<CAHolderGrainDto, CAHolderIndex>(result.Data));

            _logger.LogInformation("create holder success, userId: {userId}, aAHash: {caHash}", eventData.UserId,
                eventData.CaHash);
            await _userTokenAppService.AddUserTokenAsync(eventData.UserId, new AddUserTokenInput());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}: {Data}", "Create CA holder fail", JsonConvert.SerializeObject(eventData));
        }
    }
    
    private async Task<GuardianInfoBase> GetLoginAccountInfo(string caHash)
    {
        //if the guardian type is third party, the holderInfo is null
        var holderInfo = await _guardianProvider.GetGuardiansAsync(null, caHash);
        _logger.LogInformation("holderInfo = {0}", JsonConvert.SerializeObject(holderInfo));
        var guardianInfo = holderInfo.CaHolderInfo.FirstOrDefault(g => g.GuardianList != null
                                                                       && g.GuardianList.Guardians.Count > 0);
        if (guardianInfo == null)
        {
            return null;
        }
        _logger.LogInformation("guardianInfo = {0}", JsonConvert.SerializeObject(guardianInfo));
        GuardianInfoBase guardianInfoBase = guardianInfo?.GuardianList.Guardians.FirstOrDefault(g => g.IsLoginGuardian);
        if (guardianInfoBase == null || !guardianInfoBase.Type.Equals(((int)GuardianType.GUARDIAN_TYPE_OF_EMAIL) + ""))
        {
            return null;
        }
        var list = new List<string>();
        list.Add(guardianInfoBase.IdentifierHash);
        var hashDic = await GetIdentifiersAsync(list);
        _logger.LogInformation("hashDic = {0}", JsonConvert.SerializeObject(hashDic));
        guardianInfoBase.GuardianIdentifier = hashDic[guardianInfoBase.IdentifierHash];
        _logger.LogInformation("guardianInfoBase = {0}", JsonConvert.SerializeObject(guardianInfoBase));
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

    private async Task<string> GenerateNewAccountFormat(string nickname, GuardianInfoBase guardianInfoBase)
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
        try
        {
            if ((int)GuardianType.GUARDIAN_TYPE_OF_EMAIL == int.Parse(guardianType))
            {
                if (!guardianIdentifier.Contains("@"))
                {
                    _logger.LogInformation("nickname={0} guardianInfoBase is not login guardian", nickname);
                    return nickname;
                }
                return GetEmailFormat(nickname, guardianIdentifier);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GenerateNewAccountFormat error");
        }

        return nickname;
    }

    private async Task<string> GenerateNewAccountFormatForThirdParty(string nickname, CreateUserEto eventData)
    {
        if (eventData.ChainId.IsNullOrEmpty() || eventData.CaHash.IsNullOrEmpty())
        {
            _logger.LogWarning("third party generate account error because of chainId or cahash is null, chainId={0}, cahash={1}", eventData.ChainId, eventData.CaHash);
            return nickname;
        }

        GuardianResultDto guardianResultDto = null;
        try
        {
            GuardianIdentifierDto guardianIdentifierDto = new GuardianIdentifierDto();
            guardianIdentifierDto.ChainId = eventData.ChainId;
            guardianIdentifierDto.CaHash = eventData.CaHash;
            _logger.LogInformation("third party GuardianIdentifierDto={0}", JsonConvert.SerializeObject(guardianIdentifierDto));
            guardianResultDto = await _guardianAppService.GetGuardianIdentifiersAsync(guardianIdentifierDto);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "call GetGuardianIdentifiersAsync error, ChainId={1},CaHash={2}",  eventData.ChainId, eventData.CaHash);
        }

        if (guardianResultDto == null)
        {
            return nickname;
        }
        _logger.LogInformation("third party guardianResultDto={0}", JsonConvert.SerializeObject(guardianResultDto));
        var guardian = guardianResultDto.GuardianList.Guardians.FirstOrDefault(g => g.IsLoginGuardian);
        if (guardian == null)
        {
            return nickname;
        }
        if ("Telegram".Equals(guardian.Type))
        {
            return GetFirstNameFormat(nickname, guardian.FirstName);
        }
        return GetEmailFormat(nickname, guardian.ThirdPartyEmail);
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
        if (guardianIdentifier.IsNullOrEmpty())
        {
            return nickname;
        }
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
            return frontPart.Substring(0, 4) + GenerateAsterisk(frontLength - 4) + backPart;
        }
        else
        {
            return frontPart.Substring(0, 1) + GenerateAsterisk(frontLength - 1) + backPart;
        }
    }
    
    private string GetFirstNameFormat(string nickname, string firstName)
    {
        if (firstName.IsNullOrEmpty())
        {
            return nickname;
        }
        return firstName + "***";
    }

    private string GenerateAsterisk(int num)
    {
        if (num < 0)
        {
            return string.Empty;
        }
        string result = string.Empty;
        for (int i = num - 1; i >= 0; i--)
        {
            result += "*";
        }

        return result;
    }

    public async Task HandleEventAsync(UpdateCAHolderEto eventData)
    {
        try
        {
            await _caHolderRepository.UpdateAsync(_objectMapper.Map<UpdateCAHolderEto, CAHolderIndex>(eventData));
            _logger.LogInformation("caHolder wallet name update success, id: {id}", eventData.Id);

            var contacts = await _contactProvider.GetAddedContactsAsync(eventData.UserId);
            if (contacts == null || contacts.Count == 0) return;

            foreach (var contact in contacts)
            {
                if (contact.CaHolderInfo == null) return;
                var grain = _clusterClient.GetGrain<IContactGrain>(contact.Id);
                var updateResult = await grain.UpdateContactInfo(eventData.Nickname, eventData.Avatar);

                if (!updateResult.Success)
                {
                    _logger.LogWarning("contact wallet name update fail, contactId: {id}, message:{message}",
                        contact.Id, updateResult.Message);
                    break;
                }

                contact.CaHolderInfo.WalletName = eventData.Nickname;
                contact.Avatar = eventData.Avatar;
                contact.ModificationTime = DateTime.UtcNow;
                contact.Index = updateResult.Data.Index;

                await _contactRepository.UpdateAsync(contact);
                _logger.LogInformation("contact wallet name update success, contactId: {id}", contact.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "update nick name error, id:{id}, nickName:{nickName}, userId:{userId}",
                eventData.Id.ToString(), eventData.Nickname, eventData.UserId.ToString());
        }
    }

    public async Task HandleEventAsync(DeleteCAHolderEto eventData)
    {
        try
        {
            await _caHolderRepository.UpdateAsync(_objectMapper.Map<DeleteCAHolderEto, CAHolderIndex>(eventData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete holder error, userId: {userId}", eventData.UserId.ToString());
        }
    }
}
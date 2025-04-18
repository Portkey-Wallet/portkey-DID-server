using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.CAAccount.Provider;
using CAServer.Contacts;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains.Grain.Contacts;
using CAServer.Guardian;
using CAServer.Guardian.Provider;
using CAServer.Options;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using GuardianInfoBase = CAServer.Guardian.GuardianInfoBase;
using GuardianType = CAServer.Account.GuardianType;

namespace CAServer.EntityEventHandler.Core.Service;

public interface ICaHolderService
{
    Task CreateUserAsync(CreateUserEto eventData);
    Task UpdateCaHolderAsync(UpdateCAHolderEto eventData);
    Task DeleteCaHolderAsync(DeleteCAHolderEto eventData);
}

public class CaHolderService : ICaHolderService, ISingletonDependency
{
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CaHolderService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IUserTokenAppService _userTokenAppService;
    private readonly IContactProvider _contactProvider;
    private readonly INESTRepository<ContactIndex, Guid> _contactRepository;
    private readonly IGuardianProvider _guardianProvider;
    private readonly INESTRepository<UserExtraInfoIndex, string> _userExtraInfoRepository;
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;
    private readonly IGuardianAppService _guardianAppService;
    private readonly IUserProfilePictureProvider _userProfilePictureProvider;
    private readonly Random _random;
    private readonly IContactAppService _contactAppService;

    public CaHolderService(INESTRepository<CAHolderIndex, Guid> caHolderRepository,
        IObjectMapper objectMapper,
        ILogger<CaHolderService> logger,
        IClusterClient clusterClient,
        IUserTokenAppService userTokenAppService,
        IContactProvider contactProvider,
        INESTRepository<ContactIndex, Guid> contactRepository,
        IGuardianProvider guardianProvider,
        INESTRepository<UserExtraInfoIndex, string> userExtraInfoRepository,
        INESTRepository<GuardianIndex, string> guardianRepository,
        IGuardianAppService guardianAppService,
        IUserProfilePictureProvider userProfilePictureProvider,
        IContactAppService contactAppService)
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
        _userProfilePictureProvider = userProfilePictureProvider;
        _contactAppService = contactAppService;
        _random = new Random();
    }

    public async Task CreateUserAsync(CreateUserEto eventData)
    {
        var changedNickname = string.Empty;
        var identifierHash = string.Empty;
        var nickname = eventData.UserId.ToString("N").Substring(0, 8);
        try
        {
            var loginGuardianInfoBase = await GetLoginAccountInfo(eventData.CaHash);
            if (loginGuardianInfoBase == null)
            {
                var (name, hash) = await GenerateNewAccountFormatForThirdParty(nickname, eventData);
                changedNickname = name;
                identifierHash = hash;
            }
            else
            {
                changedNickname = await GenerateNewAccountFormat(nickname, loginGuardianInfoBase);
                identifierHash = loginGuardianInfoBase.IdentifierHash;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GenerateNewAccountFormat error, userId={0}, caHash={1}", eventData.UserId,
                eventData.CaHash);
        }

        try
        {
            var grain = _clusterClient.GetGrain<ICAHolderGrain>(eventData.UserId);
            var caHolderGrainDto = _objectMapper.Map<CreateUserEto, CAHolderGrainDto>(eventData);
            var pictures = _userProfilePictureProvider.GetDefaultUserPictures();
            string picture;
            if (!pictures.IsNullOrEmpty())
            {
                picture = pictures[_random.Next(pictures.Count)];
            }
            else
            {
                picture = string.Empty;
            }

            if (changedNickname.IsNullOrEmpty() || nickname.Equals(changedNickname))
            {
                caHolderGrainDto.Nickname = nickname;
                caHolderGrainDto.PopedUp = false;
                caHolderGrainDto.ModifiedNickname = false;
                caHolderGrainDto.Avatar = picture;
            }
            else
            {
                caHolderGrainDto.Nickname = changedNickname;
                caHolderGrainDto.PopedUp = true;
                caHolderGrainDto.ModifiedNickname = true;
                caHolderGrainDto.IdentifierHash = identifierHash;
                caHolderGrainDto.Avatar = picture;
            }

            var result = await grain.AddHolderWithAvatarAsync(caHolderGrainDto);
            if (!result.Success)
            {
                _logger.LogError("create holder fail: {message}, userId: {userId}, aAHash: {caHash}", result.Message,
                    eventData.UserId, eventData.CaHash);
                return;
            }

            var index = _objectMapper.Map<CAHolderGrainDto, CAHolderIndex>(result.Data);
            await _caHolderRepository.AddAsync(index);
            await _userTokenAppService.AddUserTokenAsync(eventData.UserId, new AddUserTokenInput());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HandleMessageError] type:{type}, data:{data}, errMsg:{errMsg}",
                eventData.GetType().Name, JsonConvert.SerializeObject(eventData), ex.StackTrace ?? "-");
        }
    }

    public async Task UpdateCaHolderAsync(UpdateCAHolderEto eventData)
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
            _logger.LogError(ex, "[HandleMessageError] type:{type}, data:{data}, errMsg:{errMsg}",
                eventData.GetType().Name, JsonConvert.SerializeObject(eventData), ex.StackTrace ?? "-");
        }
    }

    public async Task DeleteCaHolderAsync(DeleteCAHolderEto eventData)
    {
        try
        {
            await _caHolderRepository.UpdateAsync(_objectMapper.Map<DeleteCAHolderEto, CAHolderIndex>(eventData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HandleMessageError] type:{type}, data:{data}, errMsg:{errMsg}",
                eventData.GetType().Name, JsonConvert.SerializeObject(eventData), ex.StackTrace ?? "-");
        }
    }

    private async Task<GuardianInfoBase> GetLoginAccountInfo(string caHash)
    {
        //if the guardian type is third party, the holderInfo is null
        var holderInfo = await _guardianProvider.GetGuardiansAsync(null, caHash);
        var guardianInfo = holderInfo.CaHolderInfo.FirstOrDefault(g => g.GuardianList != null
                                                                       && g.GuardianList.Guardians.Count > 0);
        if (guardianInfo == null)
        {
            return null;
        }

        var guardianInfoBase = guardianInfo?.GuardianList.Guardians.FirstOrDefault(g => g.IsLoginGuardian);
        if (guardianInfoBase == null || !guardianInfoBase.Type.Equals(((int)GuardianType.GUARDIAN_TYPE_OF_EMAIL) + ""))
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

    private async Task<string> GenerateNewAccountFormat(string nickname, GuardianInfoBase guardianInfoBase)
    {
        if (guardianInfoBase == null)
        {
            return nickname;
        }

        if (!guardianInfoBase.IsLoginGuardian)
        {
            return nickname;
        }

        var guardianIdentifier = guardianInfoBase.GuardianIdentifier;
        var guardianType = guardianInfoBase.Type;
        if (guardianIdentifier == null)
        {
            return nickname;
        }

        //email  according to GuardianType
        try
        {
            if ((int)GuardianType.GUARDIAN_TYPE_OF_EMAIL == int.Parse(guardianType))
            {
                if (!guardianIdentifier.Contains("@"))
                {
                    return nickname;
                }

                return GetEmailFormat(nickname, guardianIdentifier, string.Empty, string.Empty);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GenerateNewAccountFormat error");
        }

        return nickname;
    }

    private async Task<Tuple<string, string>> GenerateNewAccountFormatForThirdParty(string nickname,
        CreateUserEto eventData)
    {
        if (eventData.ChainId.IsNullOrEmpty() || eventData.CaHash.IsNullOrEmpty())
        {
            return new Tuple<string, string>(nickname, string.Empty);
        }

        GuardianResultDto guardianResultDto = null;
        try
        {
            var guardianIdentifierDto = new GuardianIdentifierDto();
            guardianIdentifierDto.ChainId = eventData.ChainId;
            guardianIdentifierDto.CaHash = eventData.CaHash;
            guardianResultDto = await _guardianAppService.GetGuardianIdentifiersAsync(guardianIdentifierDto);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "call GetGuardianIdentifiersAsync error, ChainId={1},CaHash={2}", eventData.ChainId,
                eventData.CaHash);
        }

        if (guardianResultDto == null)
        {
            return new Tuple<string, string>(nickname, string.Empty);
        }

        var guardian = guardianResultDto.GuardianList.Guardians.FirstOrDefault(g => g.IsLoginGuardian);
        if (guardian == null)
        {
            return new Tuple<string, string>(nickname, string.Empty);
        }

        string address = string.Empty;
        if (!guardianResultDto.CaAddress.IsNullOrEmpty())
        {
            address = guardianResultDto.CaAddress;
        }

        if ("Telegram".Equals(guardian.Type) || "Twitter".Equals(guardian.Type) || "Facebook".Equals(guardian.Type))
        {
            return new Tuple<string, string>(GetFirstNameFormat(nickname, guardian.FirstName, address),
                guardian.IdentifierHash);
        }

        if ("Email".Equals(guardian.Type) && !guardian.GuardianIdentifier.IsNullOrEmpty())
        {
            return new Tuple<string, string>(
                GetEmailFormat(nickname, guardian.GuardianIdentifier, guardian.FirstName, address),
                guardian.IdentifierHash);
        }

        return new Tuple<string, string>(
            GetEmailFormat(nickname, guardian.ThirdPartyEmail, guardian.FirstName, address), guardian.IdentifierHash);
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

    private string GetEmailFormat(string nickname, string guardianIdentifier, string firstName, string address)
    {
        if (guardianIdentifier.IsNullOrEmpty())
        {
            return GetFirstNameFormat(nickname, firstName, address);
        }

        var index = guardianIdentifier.LastIndexOf("@");
        if (index < 0)
        {
            return nickname;
        }

        var frontPart = guardianIdentifier.Substring(0, index);
        var backPart = guardianIdentifier.Substring(index);
        var frontLength = frontPart.Length;
        if (frontLength > 4)
        {
            return frontPart.Substring(0, 4) + "***" + backPart;
        }
        else
        {
            return frontPart + "***" + backPart;
        }
    }

    private string GetFirstNameFormat(string nickname, string firstName, string address)
    {
        if (firstName.IsNullOrEmpty() && address.IsNullOrEmpty())
        {
            return nickname;
        }

        if (!firstName.IsNullOrEmpty() && Regex.IsMatch(firstName, "^\\w+$"))
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
}
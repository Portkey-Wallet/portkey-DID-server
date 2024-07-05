using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains;
using CAServer.Grains.Grain.Contacts;
using CAServer.ImUser.Dto;
using CAServer.Monitor.Interceptor;
using CAServer.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;
using Environment = CAServer.Options.Environment;

namespace CAServer.Contacts;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ContactAppService : CAServerAppService, IContactAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IContactProvider _contactProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ImServerOptions _imServerOptions;
    private readonly IHttpClientService _httpClientService;
    private readonly VariablesOptions _variablesOptions;
    private readonly HostInfoOptions _hostInfoOptions;
    private readonly IImRequestProvider _imRequestProvider;
    private readonly IContractProvider _contractProvider;
    private readonly ChainOptions _chainOptions;
    private readonly ChatBotOptions _chatBotOptions;
    private readonly INESTRepository<ContactIndex, Guid> _contactRepository;
    private readonly ILogger<ContactAppService> _logger;
    

    public ContactAppService(IDistributedEventBus distributedEventBus,
        IClusterClient clusterClient,
        IHttpContextAccessor httpContextAccessor,
        IContactProvider contactProvider,
        IOptionsSnapshot<ImServerOptions> imServerOptions,
        IHttpClientService httpClientService,
        IOptions<VariablesOptions> variablesOptions,
        IOptionsSnapshot<HostInfoOptions> hostInfoOptions,
        IImRequestProvider imRequestProvider,
        IOptionsSnapshot<ChainOptions> chainOptions,
        IContractProvider contractProvider, 
        IOptionsSnapshot<ChatBotOptions> chatBotOptions, INESTRepository<ContactIndex, Guid> contactRepository, ILogger<ContactAppService> logger)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _contactProvider = contactProvider;
        _variablesOptions = variablesOptions.Value;
        _httpContextAccessor = httpContextAccessor;
        _imServerOptions = imServerOptions.Value;
        _hostInfoOptions = hostInfoOptions.Value;
        _httpClientService = httpClientService;
        _imRequestProvider = imRequestProvider;
        _contractProvider = contractProvider;
        _contactRepository = contactRepository;
        _logger = logger;
        _chatBotOptions = chatBotOptions.Value;
        _chainOptions = chainOptions.Value;
    }

    [Monitor]
    public async Task<ContactResultDto> CreateAsync(CreateUpdateContactDto input)
    {
        var userId = CurrentUser.GetId();
        var existed = await CheckExistAsync(userId, input.Name);
        if (existed)
        {
            throw new UserFriendlyException(ContactMessage.ExistedMessage);
        }
        
        await CheckAddressAsync(userId, input.Addresses, input.RelationId);
        var contactDto = await GetContactDtoAsync(input);
        await CheckContactAsync(contactDto);

        var contactGrain = _clusterClient.GetGrain<IContactGrain>(GuidGenerator.Create());
        var result =
            await contactGrain.AddContactAsync(userId,
                ObjectMapper.Map<ContactDto, ContactGrainDto>(contactDto));

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        // follow
        await _distributedEventBus.PublishAsync(ObjectMapper.Map<ContactGrainDto, ContactCreateEto>(result.Data));
        var contactResultDto = ObjectMapper.Map<ContactGrainDto, ContactResultDto>(result.Data);
        var imageMap = _variablesOptions.ImageMap;

        foreach (var contactAddressDto in contactResultDto.Addresses)
        {
            contactAddressDto.ChainName = contactAddressDto.ChainName.IsNullOrWhiteSpace()
                ? CommonConstant.ChainName
                : contactAddressDto.ChainName;

            contactAddressDto.Image = imageMap.GetOrDefault(contactAddressDto.ChainName);
        }

        _ = FollowAsync(contactResultDto?.Addresses?.FirstOrDefault()?.Address, userId);
        _ = ImRemarkAsync(contactResultDto?.Addresses?.FirstOrDefault()?.Address, userId, input.Name);

        return contactResultDto;
    }

    [Monitor]
    public async Task<ContactResultDto> UpdateAsync(Guid id, CreateUpdateContactDto input)
    {
        var userId = CurrentUser.GetId();
        var contactIndex = await _contactProvider.GetContactByIdAsync(id);
        if (contactIndex.ContactType == 1)
        {
            contactIndex.Name = input.Name;
            await _contactRepository.UpdateAsync(contactIndex);
            await ImRemarkAsync(contactIndex?.ImInfo?.RelationId, userId, input.Name);
            return ObjectMapper.Map<ContactIndex, ContactResultDto>(contactIndex);
        }

        var contactGrain = _clusterClient.GetGrain<IContactGrain>(id);
        var contactResult = await contactGrain.GetContactAsync();
        if (!contactResult.Success)
        {
            throw new UserFriendlyException(contactResult.Message);
        }

        var contact = contactResult.Data;
        if (contact.Addresses != null && contact.Addresses.Count > 1 && input.Addresses != null &&
            input.Addresses.Count == 1)
        {
            throw new UserFriendlyException("can not modify address");
        }

        var isUpdate = false;
        if (contact.Addresses != null && contact.Addresses.Count == 1 && input.Addresses != null &&
            input.Addresses.Count == 1)
        {
            var addrInput = input.Addresses.First();
            var addrContact = contact.Addresses.First();
            if (addrInput.Address == addrContact.Address && addrInput.ChainId == addrContact.ChainId)
            {
                isUpdate = true;
            }
        }

        await CheckAddressAsync(userId, input.Addresses, input.RelationId, id, isUpdate);
        var contactDto = await GetContactDtoAsync(input, id);
        await CheckContactAsync(contactDto);

        var result =
            await contactGrain.UpdateContactAsync(userId,
                ObjectMapper.Map<ContactDto, ContactGrainDto>(contactDto));
        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<ContactGrainDto, ContactUpdateEto>(result.Data));

        var contactResultDto = ObjectMapper.Map<ContactGrainDto, ContactResultDto>(result.Data);
        var imageMap = _variablesOptions.ImageMap;

        foreach (var contactAddressDto in contactResultDto.Addresses)
        {
            contactAddressDto.ChainName = contactAddressDto.ChainName.IsNullOrWhiteSpace()
                ? CommonConstant.ChainName
                : contactAddressDto.ChainName;

            contactAddressDto.Image = imageMap.GetOrDefault(contactAddressDto.ChainName);
        }

        if (contact.Name != input.Name)
        {
            await ImRemarkAsync(contactResultDto?.ImInfo?.RelationId, userId, input.Name);
        }

        return contactResultDto;
    }

    [Monitor]
    public async Task DeleteAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var contact = await _contactProvider.GetContactByIdAsync(id);
        if (contact.ContactType == 1)
        {
            contact.IsDeleted = true;
            await _contactRepository.AddOrUpdateAsync(contact);
            await ImRemarkAsync(contact.ImInfo.RelationId, userId, "");
            // _ = UnFollowAsync(result.Data?.Addresses?.FirstOrDefault()?.Address, userId);
        }
        var contactGrain = _clusterClient.GetGrain<IContactGrain>(id);

        var result = await contactGrain.DeleteContactAsync(userId);
        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<ContactGrainDto, ContactUpdateEto>(result.Data));

        await ImRemarkAsync(result?.Data?.ImInfo?.RelationId, userId, "");
        _ = UnFollowAsync(result.Data?.Addresses?.FirstOrDefault()?.Address, userId);
    }

    public async Task<ContractExistDto> GetExistAsync(string name)
    {
        var userId = CurrentUser.GetId();
        var contactNameGrain =
            _clusterClient.GetGrain<IContactNameGrain>(GrainIdHelper.GenerateGrainId(userId.ToString("N"), name));
        var existed = await contactNameGrain.IsNameExist(name);

        return new ContractExistDto
        {
            Existed = existed
        };
    }

    [Monitor]
    public async Task<ContactResultDto> GetAsync(Guid id)
    {
        // var contactGrain = _clusterClient.GetGrain<IContactGrain>(id);
        //
        // var result = await contactGrain.GetContactAsync();
        // if (!result.Success)
        // {
        //     throw new UserFriendlyException(result.Message);
        // }

        var result = await _contactProvider.GetContactByIdAsync(id);
        return ObjectMapper.Map<ContactIndex, ContactResultDto>(result);
    }

    [Monitor]
    public async Task<PagedResultDto<ContactListDto>> GetListAsync(ContactGetListDto input)
    {
        var (totalCount, contactList) = await _contactProvider.GetListAsync(CurrentUser.GetId(), input);
        var contactDtoList = ObjectMapper.Map<List<ContactIndex>, List<ContactResultDto>>(contactList);


        var imageMap = _variablesOptions.ImageMap;

        foreach (var contactAddressDto in contactDtoList.SelectMany(contactProfileDto => contactProfileDto.Addresses))
        {
            contactAddressDto.ChainName = contactAddressDto.ChainName.IsNullOrWhiteSpace()
                ? CommonConstant.ChainName
                : contactAddressDto.ChainName;

            contactAddressDto.Image = imageMap.GetOrDefault(contactAddressDto.ChainName);
        }

        contactDtoList?.ForEach(t => { t.Addresses = t.Addresses?.OrderBy(f => f.ChainId).ToList(); });
        return new PagedResultDto<ContactListDto>
        {
            TotalCount = totalCount,
            Items = ObjectMapper.Map<List<ContactResultDto>, List<ContactListDto>>(contactDtoList)
        };
    }

    public async Task<ContactImputationDto> GetImputationAsync()
    {
        var isImputation = await _contactProvider.GetImputationAsync(CurrentUser.GetId());
        return new ContactImputationDto
        {
            IsImputation = isImputation
        };
    }

    [Monitor]
    public async Task ReadImputationAsync(ReadImputationDto input)
    {
        var contactGrain = _clusterClient.GetGrain<IContactGrain>(input.ContactId);
        var result = await contactGrain.ReadImputation();

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<ContactGrainDto, ContactUpdateEto>(result.Data));
    }

    [Monitor]
    public async Task<ContactResultDto> GetContactAsync(Guid contactUserId)
    {
        var contact = await _contactProvider.GetContactAsync(CurrentUser.GetId(), contactUserId);
        if (contact != null)
        {
            contact.Addresses = contact.Addresses?.OrderBy(t => t.ChainId).ToList();
            return ObjectMapper.Map<ContactIndex, ContactResultDto>(contact);
        }

        var holderInfo = await GetHolderInfoAsync(contactUserId);
        if (holderInfo != null)
        {
            contact = new ContactIndex()
            {
                Name = holderInfo.WalletName
            };
        }

        return ObjectMapper.Map<ContactIndex, ContactResultDto>(contact);
    }

    private async Task<bool> CheckExistAsync(Guid userId, string name)
    {
        if (name.IsNullOrWhiteSpace()) return false;

        var contactNameGrain =
            _clusterClient.GetGrain<IContactNameGrain>(GrainIdHelper.GenerateGrainId(userId.ToString("N"), name));
        return await contactNameGrain.IsNameExist(name);
    }

    private async Task CheckAddressAsync(Guid userId, List<ContactAddressDto> addresses, string relationId,
        Guid? contactId = null, bool isUpdate = false)
    {
        if (!relationId.IsNullOrWhiteSpace() && contactId.HasValue && contactId.Value != Guid.Empty)
        {
            return;
        }

        if (!relationId.IsNullOrWhiteSpace())
        {
            var contactRelation = await _contactProvider.GetContactByRelationIdAsync(userId, relationId);
            if (contactRelation != null)
            {
                throw new UserFriendlyException("This address has already been taken in other contacts");
            }

            return;
        }

        var address = addresses.First();

        // check self
        var holder = await _contactProvider.GetCaHolderAsync(userId, string.Empty);
        if (holder == null)
        {
            throw new UserFriendlyException("Holder not found");
        }

        var guardianDto = await _contactProvider.GetCaHolderInfoAsync(new List<string>() { }, holder.CaHash);
        if (guardianDto.CaHolderInfo.Select(t => t.CaAddress).ToList().Contains(address.Address))
        {
            throw new UserFriendlyException("Unable to add yourself to your Contacts");
        }

        if (isUpdate) return;

        // check if address already exist
        var contact = await _contactProvider.GetContactByAddressAsync(userId, address.Address);
        if (contact != null)
        {
            throw new UserFriendlyException("This address has already been taken in other contacts");
        }
    }

    private async Task<ContactDto> GetContactDtoAsync(CreateUpdateContactDto input, Guid? contactId = null)
    {
        var contact = ObjectMapper.Map<CreateUpdateContactDto, ContactDto>(input);
        if (input.Addresses.Count == 0 && !input.RelationId.IsNullOrWhiteSpace())
        {
            if (contactId.HasValue && contactId.Value != Guid.Empty)
            {
                return contact;
            }

            var userInfo = await GetImInfoAsync(input.RelationId);
            if (userInfo != null)
            {
                contact.ImInfo = ObjectMapper.Map<ImInfoDto, ImInfo>(userInfo);
                var holderInfoWithAvatar = await GetHolderInfoAsync(userInfo.PortkeyId);
                contact.CaHolderInfo = ObjectMapper.Map<HolderInfoWithAvatar, CaHolderInfo>(holderInfoWithAvatar);
                if (contact.CaHolderInfo == null)
                {
                    contact.Addresses =
                        ObjectMapper.Map<List<AddressWithChain>, List<ContactAddressDto>>(userInfo.AddressWithChain);
                    return contact;
                }

                contact.Avatar = holderInfoWithAvatar?.Avatar;
                contact.Addresses = await GetAddressesAsync(contact.CaHolderInfo.CaHash);
            }

            return contact;
        }

        var address = input.Addresses.First();

        contact.ImInfo = await GetImUserAsync(address.Address);
        var caHolderInfo = await GetHolderInfoAsync(contact.ImInfo, input.Addresses);
        contact.CaHolderInfo = ObjectMapper.Map<HolderInfoWithAvatar, CaHolderInfo>(caHolderInfo);
        contact.Avatar = caHolderInfo?.Avatar;

        if (!address.ChainName.IsNullOrWhiteSpace() && address.ChainName != CommonConstant.ChainName) return contact;

        var caHash = contact.CaHolderInfo == null ? string.Empty : contact.CaHolderInfo.CaHash;

        var guardians =
            await _contactProvider.GetCaHolderInfoAsync(new List<string> { address.Address },
                caHash);

        if (guardians?.CaHolderInfo?.Count > 0 && contact.ImInfo != null &&
            contact.Addresses.Count < _chainOptions.ChainInfos.Keys.Count)
        {
            var chainIds = contact.Addresses.Select(t => t.ChainId);
            var needAddChainIds = _chainOptions.ChainInfos.Keys.Except(chainIds).ToList();
            
            foreach (var chainId in needAddChainIds)
            {
                contact.Addresses.Add(new ContactAddressDto()
                {
                    Address = address.Address,
                    ChainId = chainId
                });
            }

            contact.Addresses = contact.Addresses.OrderBy(t => t.ChainId).ToList();
        }

        return contact;
    }

    private async Task<List<ContactAddressDto>> GetAddressesAsync(string caHash)
    {
        var addresses = new List<ContactAddressDto>();
        var guardians =
            await _contactProvider.GetCaHolderInfoAsync(new List<string>(), caHash);

        var guardianDto = guardians?.CaHolderInfo?.FirstOrDefault();
        if (guardianDto == null)
        {
            Logger.LogError("holder info is null, caHash:{caHash}", caHash);
            return addresses;
        }

        foreach (var chainId in _chainOptions.ChainInfos.Keys)
        {
            addresses.Add(new ContactAddressDto()
            {
                Address = guardianDto.CaAddress,
                ChainId = chainId
            });
        }

        return addresses;
    }

    private async Task<HolderInfoWithAvatar> GetHolderInfoAsync(ImInfo imInfo, List<ContactAddressDto> addresses)
    {
        if (imInfo != null && imInfo.PortkeyId != Guid.Empty)
        {
            return await GetHolderInfoAsync(imInfo.PortkeyId);
        }

        if (addresses == null || addresses.Count == 0) return null;

        return await GetHolderInfoAsync(addresses.First());
    }

    private async Task<HolderInfoWithAvatar> GetHolderInfoAsync(Guid userId)
    {
        if (userId == Guid.Empty) return null;

        var caHolderGrain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        var caHolder = await caHolderGrain.GetCaHolder();
        if (!caHolder.Success)
        {
            throw new UserFriendlyException(caHolder.Message);
        }

        return ObjectMapper.Map<CAHolderGrainDto, HolderInfoWithAvatar>(caHolder.Data);
    }

    private async Task<HolderInfoWithAvatar> GetHolderInfoAsync(ContactAddressDto address)
    {
        var guardiansDto =
            await _contactProvider.GetCaHolderInfoAsync(new List<string> { address.Address }, string.Empty);
        var caHash = guardiansDto?.CaHolderInfo?.FirstOrDefault()?.CaHash;
        if (caHash.IsNullOrWhiteSpace()) return null;

        var caHolder = await _contactProvider.GetCaHolderAsync(Guid.Empty, caHash);
        return ObjectMapper.Map<CAHolderIndex, HolderInfoWithAvatar>(caHolder);
    }

    public async Task<ImInfoDto> GetImInfoAsync(string relationId)
    {
        if (relationId.IsNullOrWhiteSpace()) return null;
        if (_hostInfoOptions.Environment == Environment.Development) return null;

        var hasAuthToken = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(CommonConstant.AuthHeader,
            out var authToken);
        var header = new Dictionary<string, string>();
        if (hasAuthToken)
        {
            header.Add(CommonConstant.AuthHeader, authToken);
        }

        var url = _imServerOptions.BaseUrl + $"api/v1/users/imUserInfo?relationId={relationId}";
        var responseDto = await _httpClientService.GetAsync<CommonResponseDto<ImInfoDto>>(url, header);
        if (!responseDto.Success)
        {
            throw new UserFriendlyException(responseDto.Message);
        }

        return responseDto.Data;
    }

    private async Task<ImInfo> GetImUserAsync(string address)
    {
        if (address.IsNullOrWhiteSpace()) return null;

        if (_hostInfoOptions.Environment == Environment.Development) return null;

        var hasAuthToken = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(CommonConstant.AuthHeader,
            out var authToken);

        var header = new Dictionary<string, string>();
        if (hasAuthToken)
        {
            header.Add(CommonConstant.AuthHeader, authToken);
        }

        var url = _imServerOptions.BaseUrl + $"api/v1/users/imUser?address={address}";
        var responseDto = await _httpClientService.GetAsync<CommonResponseDto<ImInfo>>(url, header);
        if (!responseDto.Success)
        {
            throw new UserFriendlyException(responseDto.Message);
        }

        return responseDto.Data;
    }


    private async Task ImRemarkAsync(string relationId, Guid userId, string name)
    {
        if (_hostInfoOptions.Environment == Environment.Development)
        {
            return;
        }

        var imRemarkDto = new ImRemarkDto
        {
            Remark = name,
            RelationId = relationId
        };

        try
        {
            await _imRequestProvider.PostAsync<object>(ImConstant.ImRemarkUrl, imRemarkDto);
            Logger.LogInformation("{userId} remark : {relationId}, {name}", userId.ToString(), relationId, name);
        }
        catch (Exception e)
        {
            Logger.LogError(e,
                ImConstant.ImServerErrorPrefix + " remark fail : {userId}, {relationId}, {name}, {imToken}",
                userId.ToString(), relationId, name,
                _httpContextAccessor?.HttpContext?.Request?.Headers[CommonConstant.ImAuthHeader]);
        }
    }

    private async Task FollowAsync(string address, Guid userId)
    {
        try
        {
            if (address.IsNullOrWhiteSpace()) return;

            var followDto = new FollowRequestDto()
            {
                Address = address
            };

            await ImPostAsync(_imServerOptions.BaseUrl + CommonConstant.ImFollowUrl, followDto);
            Logger.LogInformation("{userId} follow address: {address}", address, userId.ToString());
        }
        catch (Exception e)
        {
            Logger.LogError(e, ImConstant.ImServerErrorPrefix + " follow fail : {userId}, {address}, {imToken}",
                userId.ToString(), address,
                _httpContextAccessor?.HttpContext?.Request?.Headers[CommonConstant.ImAuthHeader]);
        }
    }

    private async Task UnFollowAsync(string address, Guid userId)
    {
        try
        {
            if (address.IsNullOrWhiteSpace()) return;

            var followDto = new FollowRequestDto()
            {
                Address = address
            };

            await ImPostAsync(_imServerOptions.BaseUrl + CommonConstant.ImUnFollowUrl, followDto);
            Logger.LogInformation("{userId} unfollow address: {address}", address, userId.ToString());
        }
        catch (Exception e)
        {
            Logger.LogError(e, ImConstant.ImServerErrorPrefix + " unfollow fail : {userId}, {address}, {imToken}",
                userId.ToString(), address,
                _httpContextAccessor?.HttpContext?.Request?.Headers[CommonConstant.ImAuthHeader]);
        }
    }

    private async Task ImPostAsync(string url, object param)
    {
        if (_hostInfoOptions.Environment == Environment.Development) return;

        if (!_httpContextAccessor.HttpContext.Request.Headers.Keys.Contains(CommonConstant.ImAuthHeader,
                StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        var header = new Dictionary<string, string>();
        header.Add(CommonConstant.ImAuthHeader,
            _httpContextAccessor.HttpContext.Request.Headers[CommonConstant.ImAuthHeader]);

        var hasAuthToken = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(CommonConstant.AuthHeader,
            out var authToken);

        if (hasAuthToken)
        {
            header.Add(CommonConstant.AuthHeader, authToken);
        }

        var responseDto = await _httpClientService.PostAsync<CommonResponseDto<object>>(url, param, header);
        if (!responseDto.Success)
        {
            Logger.LogError("request im error, url:{url}", url);
        }
    }


    public async Task<List<GetNamesResultDto>> GetNameAsync(List<Guid> input)
    {
        var result = new List<GetNamesResultDto>();
        var userId = CurrentUser.GetId();
        var contacts = await _contactProvider.GetContactsAsync(userId);

        var contactsIm = contacts.Where(t => t.ImInfo != null).ToList();
        var names = contactsIm.Where(t => !t.Name.IsNullOrWhiteSpace());
        foreach (var contact in names)
        {
            result.Add(new GetNamesResultDto()
            {
                PortkeyId = Guid.Parse(contact.ImInfo.PortkeyId),
                Name = contact.Name,
                Avatar = contact.Avatar
            });

            input.Remove(Guid.Parse(contact.ImInfo.PortkeyId));
        }

        var contactsHolder = contactsIm.Where(t => t.Name.IsNullOrWhiteSpace() && t.CaHolderInfo != null);
        foreach (var contact in contactsHolder)
        {
            result.Add(new GetNamesResultDto()
            {
                PortkeyId = contact.CaHolderInfo.UserId,
                Name = contact.CaHolderInfo.WalletName,
                Avatar = contact.Avatar
            });

            input.Remove(contact.CaHolderInfo.UserId);
        }

        if (input.Count == 0) return result;

        var holders = await _contactProvider.GetCaHoldersAsync(input);
        foreach (var holder in holders)
        {
            result.Add(new GetNamesResultDto()
            {
                PortkeyId = holder.UserId,
                Name = holder.NickName,
                Avatar = holder.Avatar
            });

            input.Remove(holder.UserId);
        }

        foreach (var item in input)
        {
            result.Add(new GetNamesResultDto()
            {
                PortkeyId = item,
                Name = string.Empty,
                Avatar = string.Empty
            });
        }

        return result;
    }

    [Monitor]
    public async Task<List<ContactResultDto>> GetContactListAsync(ContactListRequestDto input)
    {
        var contacts =
            await _contactProvider.GetContactListAsync(input.ContactUserIds, input.Address, CurrentUser.GetId());
        if (contacts != null && contacts.Any())
        {
            contacts.ForEach(contact => contact.Addresses = contact.Addresses?.OrderBy(t => t.ChainId).ToList());
            return ObjectMapper.Map<List<ContactIndex>, List<ContactResultDto>>(contacts);
        }

        return new List<ContactResultDto>();
    }

    [Monitor]
    public async Task<List<ContactResultDto>> GetContactsByUserIdAsync(Guid userId)
    {
        var contacts= await _contactProvider.GetContactsAsync(userId);
        return ObjectMapper.Map<List<ContactIndex>, List<ContactResultDto>>(contacts);
    }

    private async Task CheckContactAsync(ContactDto contact)
    {
        if (contact.ImInfo != null && contact.CaHolderInfo == null)
        {
            throw new UserFriendlyException("add contact fail.");
        }

        if (contact.ImInfo != null && contact.Addresses.Count < _chainOptions.ChainInfos.Keys.Count)
        {
            var chainIds = contact.Addresses.Select(t => t.ChainId);

            foreach (var chainInfo in _chainOptions.ChainInfos.Where(t => !chainIds.Contains(t.Key)))
            {
                var result = await GetAddressAsync(chainInfo.Key, contact.CaHolderInfo.CaHash);
                if (result == null) continue;

                contact.Addresses.Add(new ContactAddressDto()
                {
                    Address = result.CaAddress.ToBase58(),
                    ChainId = chainInfo.Key
                });
            }
        }
    }

    private async Task<GetHolderInfoOutput> GetAddressAsync(string chainId, string caHash)
    {
        try
        {
            return await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(caHash), null, chainId);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "get holder error, caHash:{caHash}, chainId:{chainId}", caHash,
                chainId);

            return null;
        }
    }
}
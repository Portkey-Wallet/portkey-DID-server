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

namespace CAServer.Contacts;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ContactAppService : CAServerAppService, IContactAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IContactProvider _contactProvider;
    private readonly VariablesOptions _variablesOptions;
    private readonly IContractProvider _contractProvider;
    private readonly ChainOptions _chainOptions;
    private readonly INESTRepository<ContactIndex, Guid> _contactRepository;
    private readonly ILogger<ContactAppService> _logger;


    public ContactAppService(IDistributedEventBus distributedEventBus,
        IClusterClient clusterClient,
        IContactProvider contactProvider,
        IOptions<VariablesOptions> variablesOptions,
        IOptionsSnapshot<ChainOptions> chainOptions,
        IContractProvider contractProvider,
        INESTRepository<ContactIndex, Guid> contactRepository,
        ILogger<ContactAppService> logger)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _contactProvider = contactProvider;
        _variablesOptions = variablesOptions.Value;
        _contractProvider = contractProvider;
        _contactRepository = contactRepository;
        _logger = logger;
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

        var dto = ObjectMapper.Map<ContactGrainDto, ContactCreateEto>(result.Data);

        await _distributedEventBus.PublishAsync(dto);
        var contactResultDto = ObjectMapper.Map<ContactGrainDto, ContactResultDto>(result.Data);
        var imageMap = _variablesOptions.ImageMap;

        foreach (var contactAddressDto in contactResultDto.Addresses)
        {
            contactAddressDto.ChainName = contactAddressDto.ChainName.IsNullOrWhiteSpace()
                ? CommonConstant.ChainName
                : contactAddressDto.ChainName;

            contactAddressDto.Image = imageMap.GetOrDefault(contactAddressDto.ChainName);
        }
        return contactResultDto;
    }

    [Monitor]
    public async Task<ContactResultDto> UpdateAsync(Guid id, CreateUpdateContactDto input)
    {
        var userId = CurrentUser.GetId();
        var contactIndex = await _contactProvider.GetContactByIdAsync(id);
        if (contactIndex.ContactType == 1)
        {
            contactIndex.ModificationTime = DateTime.UtcNow;
            contactIndex.Name = input.Name;
            contactIndex.Index =
                GetIndex(string.IsNullOrWhiteSpace(input.Name) ? contactIndex.ImInfo.Name : input.Name);
            await _contactRepository.UpdateAsync(contactIndex);

            _logger.LogDebug("Update contact is {contact}", JsonConvert.SerializeObject(contactIndex));
            return ObjectMapper.Map<ContactIndex, ContactResultDto>(contactIndex);
        }

        var contactGrain = _clusterClient.GetGrain<IContactGrain>(id);
        var contactResult = await contactGrain.GetContactAsync();
        if (!contactResult.Success)
        {
            throw new UserFriendlyException(contactResult.Message);
        }

        var contact = contactResult.Data;
        var isUpdate = false;
        if (contact.Addresses != null && contact.Addresses.Count > 1 && input.Addresses != null)
        {
            if (input.Addresses.Count == 1)
                throw new UserFriendlyException("can not modify address");

            if (!input.Addresses.Select(t => t.Address).Distinct()
                    .Except(contact.Addresses.Select(t => t.Address).Distinct()).Any()
               )
            {
                isUpdate = true;
            }
        }

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
        
        return contactResultDto;
    }

    [Monitor]
    public async Task DeleteAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var contact = await _contactProvider.GetContactByIdAsync(id);
        _logger.LogDebug("Delete Data is {data}", JsonConvert.SerializeObject(contact));
        if (contact.ContactType == 1)
        {
            contact.IsDeleted = true;
            contact.ModificationTime = DateTime.UtcNow;
            await _contactRepository.AddOrUpdateAsync(contact);
            var updatedContact = await _contactProvider.GetContactByIdAsync(id);
            _logger.LogDebug("After Delete contact is {contact}", JsonConvert.SerializeObject(updatedContact));
            return;
        }

        var contactGrain = _clusterClient.GetGrain<IContactGrain>(id);

        var result = await contactGrain.DeleteContactAsync(userId);
        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<ContactGrainDto, ContactUpdateEto>(result.Data));
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

    public async Task<ImInfoDto> GetImInfoAsync(string relationId) => null;

    private async Task<ImInfo> GetImUserAsync(string address) => null;

    public async Task<ContactResultDto> GetContactsByRelationIdAsync(Guid userId, string relationId)
    {
        var index = await _contactProvider.GetContactByRelationIdAsync(userId, relationId);
        _logger.LogDebug("Get Contact from ES {contact}", JsonConvert.SerializeObject(index));
        return ObjectMapper.Map<ContactIndex, ContactResultDto>(index);
    }

    public async Task<ContactResultDto> GetContactsByPortkeyIdAsync(Guid userId, Guid portKeyId)
    {
        var index = await _contactProvider.GetContactByPortKeyIdAsync(userId, portKeyId.ToString());
        return ObjectMapper.Map<ContactIndex, ContactResultDto>(index);
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
        var contacts = await _contactProvider.GetContactsAsync(userId);
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

    private string GetIndex(string name)
    {
        var firstChar = char.ToUpperInvariant(name[0]);
        if (firstChar >= 'A' && firstChar <= 'Z')
        {
            return firstChar.ToString();
        }

        return "#";
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains;
using CAServer.Grains.Grain.Contacts;
using CAServer.ImUser.Dto;
using CAServer.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
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
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ImServerOptions _imServerOptions;
    private readonly IHttpClientService _httpClientService;
    private readonly VariablesOptions _variablesOptions;

    public ContactAppService(IDistributedEventBus distributedEventBus, IClusterClient clusterClient,
        IHttpContextAccessor httpContextAccessor,
        IContactProvider contactProvider,
        IOptionsSnapshot<ImServerOptions> imServerOptions,
        IHttpClientService httpClientService,
        IOptions<VariablesOptions> variablesOptions)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _contactProvider = contactProvider;
        _variablesOptions = variablesOptions.Value;
        _httpContextAccessor = httpContextAccessor;
        _imServerOptions = imServerOptions.Value;
        _httpClientService = httpClientService;
    }

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
        return ObjectMapper.Map<ContactGrainDto, ContactResultDto>(result.Data);
    }

    public async Task<ContactResultDto> UpdateAsync(Guid id, CreateUpdateContactDto input)
    {
        var userId = CurrentUser.GetId();

        await CheckAddressAsync(userId, input.Addresses, input.RelationId, id);
        var contactDto = await GetContactDtoAsync(input, id);
        var contactGrain = _clusterClient.GetGrain<IContactGrain>(id);
        var result =
            await contactGrain.UpdateContactAsync(userId,
                ObjectMapper.Map<ContactDto, ContactGrainDto>(contactDto));

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<ContactGrainDto, ContactUpdateEto>(result.Data));
        return ObjectMapper.Map<ContactGrainDto, ContactResultDto>(result.Data);
    }

    public async Task DeleteAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
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

    public async Task<ContactResultDto> GetAsync(Guid id)
    {
        var contactGrain = _clusterClient.GetGrain<IContactGrain>(id);

        var result = await contactGrain.GetContactAsync();
        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        return ObjectMapper.Map<ContactGrainDto, ContactResultDto>(result.Data);
    }

    public async Task<PagedResultDto<ContactResultDto>> GetListAsync(ContactGetListDto input)
    {
        var (totalCount, contactList) = await _contactProvider.GetListAsync(CurrentUser.GetId(), input);

        var pagedResultDto = new PagedResultDto<ContactResultDto>
        {
            TotalCount = totalCount,
            Items = ObjectMapper.Map<List<ContactIndex>, List<ContactResultDto>>(contactList)
        };

        var imageMap = _variablesOptions.ImageMap;

        foreach (var contactProfileDto in pagedResultDto.Items)
        {
            foreach (var contactAddressDto in contactProfileDto.Addresses)
            {
                contactAddressDto.Image = imageMap.GetOrDefault(contactAddressDto.ChainName);
            }
        }

        return pagedResultDto;
    }

    public async Task MergeAsync(ContactMergeDto input)
    {
        try
        {
            var userId = CurrentUser.GetId();
            // var contacts = await _contactProvider.GetContactsAsync(userId);
            //
            // if (contacts.Count == 0)
            // {
            //     return;
            // }
            //
            // var holderInfo = await GetHolderInfoAsync(userId);
            // var guardianDto = await _contactProvider.GetCaHolderInfoAsync(new List<string>(), holderInfo.CaHash);
            // var addresses = guardianDto?.CaHolderInfo?.Select(t => t.CaAddress).ToList();
            // var needDeletedContacts = await _contactProvider.GetContactByAddressesAsync(userId, addresses);
            //
            // if (needDeletedContacts is { Count: > 0 })
            // {
            //     foreach (var contact in needDeletedContacts)
            //     {
            //         await DeleteAsync(contact.Id);
            //     }
            // }

            var addresses = input.Addresses.Select(t => t.Address).ToList();
            //merge
            var rawContacts = await _contactProvider.GetContactByAddressesAsync(Guid.Empty, addresses);
            var mergeContacts = rawContacts?.Where(t => t.ImInfo == null).ToList();

            if (mergeContacts == null || mergeContacts.Count == 0)
            {
                Logger.LogInformation("[contact merge] no contact need merge, {userId}", userId.ToString());
            }

            var contacts = ObjectMapper.Map<List<ContactIndex>, List<ContactDto>>(mergeContacts);
            // linq group by  uid
            var contactGroups = contacts.GroupBy(t => t.UserId);
            foreach (var group in contactGroups)
            {
                // Whether the contact address is your own, it is your own direct deletion
                if (group.Key == userId)
                {
                    foreach (var needDeletedContact in group)
                    {
                        Logger.LogInformation(
                            "[contact merge] delete self success,userId:{userId},contactId:{contactId}",
                            userId.ToString(), needDeletedContact.Id.ToString());

                        await DeleteAsync(needDeletedContact.Id);
                    }

                    break;
                }

                //If all contacts in the address in the input address of the contact are merged into one, the other contacts are deleted
                var contactUpdate = group.Where(t => !t.Name.IsNullOrWhiteSpace()).OrderBy(t => t.Name)
                    .FirstOrDefault();

                contactUpdate.CaHolderInfo = await GetHolderInfoAsync(userId);
                contactUpdate.ImInfo = input.ImInfo;

                if (contactUpdate.CaHolderInfo == null)
                {
                    Logger.LogError("get holder error. userId:{userId}", userId.ToString());
                    break;
                }

                var guardianDto =
                    await _contactProvider.GetCaHolderInfoAsync(new List<string>(), contactUpdate.CaHolderInfo.CaHash);
                var caAddresses = guardianDto?.CaHolderInfo?.Select(t => new { t.CaAddress, t.ChainId }).ToList();

                if (caAddresses != null && caAddresses.Count > 0)
                {
                    contactUpdate.Addresses.Clear();
                    foreach (var caAddress in caAddresses)
                    {
                        contactUpdate.Addresses.Add(new ContactAddressDto()
                        {
                            Address = caAddress.CaAddress,
                            ChainId = caAddress.ChainId,
                            ChainName = CommonConstant.ChainName
                        });
                    }
                }

                await MergeUpdateAsync(contactUpdate.Id, contactUpdate);

                var needDeletes = group.Where(t => t.Id != contactUpdate.Id).ToList();
                foreach (var needDeletedContact in needDeletes)
                {
                    await DeleteAsync(needDeletedContact.Id);
                }
            }

            // record deleted contacts
        }
        catch (Exception e)
        {
            Logger.LogError($"Merge fail,{JsonConvert.SerializeObject(input)}", e);
        }
    }

    public async Task<ContactImputationDto> GetImputationAsync()
    {
        var isImputation = await _contactProvider.GetImputationAsync(CurrentUser.GetId());
        return new ContactImputationDto
        {
            IsImputation = isImputation
        };
    }

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

    public async Task<ContactResultDto> GetContactAsync(Guid contactUserId)
    {
        var contact = await _contactProvider.GetContactAsync(CurrentUser.GetId(), contactUserId);
        return ObjectMapper.Map<ContactIndex, ContactResultDto>(contact);
    }

    private async Task<bool> CheckExistAsync(Guid userId, string name)
    {
        if (name.IsNullOrWhiteSpace()) return false;

        var contactNameGrain =
            _clusterClient.GetGrain<IContactNameGrain>(GrainIdHelper.GenerateGrainId(userId.ToString("N"), name));
        return await contactNameGrain.IsNameExist(name);
    }

    //need to optimize
    private async Task CheckAddressAsync(Guid userId, List<ContactAddressDto> addresses, string relationId,
        Guid? contactId = null)
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

        //check address is aelf
        if (address.ChainName == CommonConstant.ChainName)
        {
        }

        // check if address already exist
        var contact = await _contactProvider.GetContactByAddressAsync(userId, address.Address);
        if (contact != null)
        {
            throw new UserFriendlyException("This address has already been taken in other contacts");
        }

        // var imInfo = await GetImUserAsync(address.Address);
        // if (imInfo == null || imInfo.RelationId.IsNullOrWhiteSpace()) return;
        //
        // var contactInfo = await _contactProvider.GetContactByRelationIdAsync(userId, imInfo.RelationId);
        // if (contactInfo != null)
        // {
        //     throw new UserFriendlyException("This address has already been taken in other contacts");
        // }
    }

    private async Task<ContactDto> GetContactDtoAsync(CreateUpdateContactDto input, Guid? contactId = null)
    {
        //return ObjectMapper.Map<CreateUpdateContactDto, ContactDto>(input);
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
                contact.ImInfo = userInfo;
            }

            return contact;
        }

        var address = input.Addresses.First();

        contact.ImInfo = await GetImUserAsync(address.Address);
        contact.CaHolderInfo = await GetHolderInfoAsync(contact.ImInfo, input.Addresses);

        if (!address.ChainName.IsNullOrWhiteSpace() && address.ChainName != CommonConstant.ChainName) return contact;

        var caHash = contact.CaHolderInfo == null ? string.Empty : contact.CaHolderInfo.CaHash;

        var guardians =
            await _contactProvider.GetCaHolderInfoAsync(new List<string> { address.Address },
                caHash);

        if (guardians?.CaHolderInfo?.Count > 0)
        {
            var addressInfos = guardians.CaHolderInfo.Where(t => t.CaAddress == address.Address)
                .Select(t => new { t.CaAddress, t.ChainId }).FirstOrDefault();

            if (addressInfos != null && addressInfos.ChainId != address.ChainId)
            {
                throw new UserFriendlyException("Invalid address");
            }

            if (contact.ImInfo != null)
            {
                var addAddressInfos = guardians.CaHolderInfo.Where(t => t.CaAddress != address.Address)
                    .Select(t => new { t.CaAddress, t.ChainId });

                foreach (var info in addAddressInfos)
                {
                    contact.Addresses.Add(new ContactAddressDto()
                    {
                        Address = info.CaAddress,
                        ChainId = info.ChainId
                    });
                }
            }
        }

        return contact;
    }

    private async Task<CaHolderInfo> GetHolderInfoAsync(ImInfo imInfo, List<ContactAddressDto> addresses)
    {
        if (imInfo != null && imInfo.PortkeyId != Guid.Empty)
        {
            return await GetHolderInfoAsync(imInfo.PortkeyId);
        }

        if (addresses == null || addresses.Count == 0) return null;

        return await GetHolderInfoAsync(addresses.First());
    }

    private async Task<CaHolderInfo> GetHolderInfoAsync(Guid userId)
    {
        if (userId == Guid.Empty) return null;

        var caHolderGrain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        var caHolder = await caHolderGrain.GetCaHolder();
        if (!caHolder.Success)
        {
            throw new UserFriendlyException(caHolder.Message);
        }

        return ObjectMapper.Map<CAHolderGrainDto, CaHolderInfo>(caHolder.Data);
    }

    private async Task<CaHolderInfo> GetHolderInfoAsync(ContactAddressDto address)
    {
        var guardiansDto =
            await _contactProvider.GetCaHolderInfoAsync(new List<string> { address.Address }, string.Empty);
        var caHash = guardiansDto?.CaHolderInfo?.FirstOrDefault()?.CaHash;
        if (caHash.IsNullOrWhiteSpace()) return null;

        var caHolder = await _contactProvider.GetCaHolderAsync(Guid.Empty, caHash);
        return ObjectMapper.Map<CAHolderIndex, CaHolderInfo>(caHolder);
    }

    private async Task<ImInfo> GetImInfoAsync(string relationId)
    {
        if (relationId.IsNullOrWhiteSpace()) return null;
        var hasAuthToken = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(CommonConstant.AuthHeader,
            out var authToken);


        var header = new Dictionary<string, string>();
        if (hasAuthToken)
        {
            header.Add(CommonConstant.AuthHeader, authToken);
        }

        var responseDto = await _httpClientService.GetAsync<CommonResponseDto<ImInfo>>(
            _imServerOptions.BaseUrl + $"api/v1/users/imUserInfo?relationId={relationId}",
            header);

        if (!responseDto.Success())
        {
            throw new UserFriendlyException(responseDto.Message);
        }

        return responseDto.Data;
    }

    private async Task<ImInfo> GetImUserAsync(string address)
    {
        if (address.IsNullOrWhiteSpace()) return null;
        var hasAuthToken = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(CommonConstant.AuthHeader,
            out var authToken);


        var header = new Dictionary<string, string>();
        if (hasAuthToken)
        {
            header.Add(CommonConstant.AuthHeader, authToken);
        }

        var responseDto = await _httpClientService.GetAsync<CommonResponseDto<ImInfo>>(
            _imServerOptions.BaseUrl + $"api/v1/users/imUser?address={address}",
            header);

        if (!responseDto.Success())
        {
            throw new UserFriendlyException(responseDto.Message);
        }

        return responseDto.Data;
    }


    public async Task<List<GetNamesResultDto>> GetNameAsync(List<Guid> input)
    {
        var result = new List<GetNamesResultDto>();
        var userId = CurrentUser.GetId();
        var contacts = await _contactProvider.GetContactsAsync(userId);

        var contas = contacts.Where(t => t.ImInfo != null).ToList();
        var names = contas.Where(t => !t.Name.IsNullOrWhiteSpace());
        foreach (var name in names)
        {
            result.Add(new GetNamesResultDto()
            {
                PortkeyId = Guid.Parse(name.ImInfo.PortkeyId),
                Name = name.Name
            });

            input.Remove(Guid.Parse(name.ImInfo.PortkeyId));
        }

        var names2 = contas.Where(t => t.Name.IsNullOrWhiteSpace() && t.CaHolderInfo != null);
        foreach (var name in names2)
        {
            result.Add(new GetNamesResultDto()
            {
                PortkeyId = name.CaHolderInfo.UserId,
                Name = name.CaHolderInfo.WalletName
            });

            input.Remove(name.CaHolderInfo.UserId);
        }


        if (input.Count == 0) return result;

        var holders = await _contactProvider.GetCaHoldersAsync(input);
        foreach (var holder in holders)
        {
            result.Add(new GetNamesResultDto()
            {
                PortkeyId = holder.UserId,
                Name = holder.NickName
            });

            input.Remove(holder.UserId);
        }

        foreach (var per in input)
        {
            result.Add(new GetNamesResultDto()
            {
                PortkeyId = per,
                Name = string.Empty
            });
        }

        return result;
    }

    public async Task<ContactResultDto> MergeUpdateAsync(Guid id, ContactDto contactDto)
    {
        var userId = CurrentUser.GetId();

        var contactGrain = _clusterClient.GetGrain<IContactGrain>(id);
        var result =
            await contactGrain.UpdateContactAsync(userId,
                ObjectMapper.Map<ContactDto, ContactGrainDto>(contactDto));

        if (!result.Success)
        {
            Logger.LogError("Imputation fail, contactId:{id}, message:{message}", id.ToString(), result.Message);
            return null;
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<ContactGrainDto, ContactUpdateEto>(result.Data), false,
            false);

        var imputationResult = await contactGrain.Imputation();
        if (!imputationResult.Success)
        {
            Logger.LogError("Imputation fail, contactId:{id}", id.ToString());
            return null;
        }

        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<ContactGrainDto, ContactUpdateEto>(imputationResult.Data), false,
            false);

        return ObjectMapper.Map<ContactGrainDto, ContactResultDto>(imputationResult.Data);
    }
}
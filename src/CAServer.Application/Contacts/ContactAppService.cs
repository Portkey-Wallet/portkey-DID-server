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

    public ContactAppService(IDistributedEventBus distributedEventBus, IClusterClient clusterClient,
        IHttpContextAccessor httpContextAccessor,
        IContactProvider contactProvider,
        IOptionsSnapshot<ImServerOptions> imServerOptions,
        IHttpClientService httpClientService)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _contactProvider = contactProvider;
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

        var contactGrain = _clusterClient.GetGrain<IContactGrain>(id);
        var result =
            await contactGrain.UpdateContactAsync(userId,
                ObjectMapper.Map<CreateUpdateContactDto, ContactGrainDto>(input));

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
        input.UserId = CurrentUser.GetId();
        var (totalCount, contactList) = await _contactProvider.GetListAsync(input);

        var pagedResultDto = new PagedResultDto<ContactResultDto>
        {
            TotalCount = totalCount,
            Items = ObjectMapper.Map<List<ContactIndex>, List<ContactResultDto>>(contactList)
        };

        return pagedResultDto;
    }

    public async Task MergeAsync(ContactMergeDto input)
    {
        var userId = CurrentUser.GetId();
        var contacts = await _contactProvider.GetContactsAsync(userId);

        if (contacts.Count == 0)
        {
            return;
        }

        contacts = contacts.Where(t => t.ImInfo == null).ToList();

        // 查询联系人地址是否是自己的，是自己的直接删除
        // foreach (var contact in contacts)
        // {
        //     if(contact.Addresses)
        // }
        // var holder = await GetHolderInfoAsync(userId);
        // var guardians =
        //     await _contactProvider.GetCaHolderInfoAsync(new List<string> { address.Address },
        //         contact.CaHolderInfo.CaHash);
        //
        // if (guardians?.CaHolderInfo?.Count > 0)
        // {
        //     var addressInfos = guardians.CaHolderInfo.Where(t => t.CaAddress != address.Address)
        //         .Select(t => new { t.CaAddress, t.ChainId });
        //
        //     foreach (var info in addressInfos)
        //     {
        //         contact.Addresses.Add(new ContactAddressDto()
        //         {
        //             Address = info.CaAddress,
        //             ChainId = info.ChainId
        //         });
        //     }
        // }

        // 若联系人的address中input中的address的所有联系人合并为1个、删除其它联系人
        foreach (var contact in contacts)
        {
            if (contact.Addresses is { Count: 1 })
            {
                var imInfo = await GetImUserAsync(contact.Addresses.First().Address);
                if (imInfo == null || imInfo.RelationId.IsNullOrWhiteSpace()) continue;

                var contactRelation = await _contactProvider.GetContactByRelationIdAsync(userId, imInfo.RelationId);
                if (contactRelation == null) continue;

                contactRelation.Addresses.Add(new CAServer.Entities.Es.ContactAddress()
                {
                    ChainName = contact.Addresses.First().ChainName,
                    ChainId = contact.Addresses.First().ChainId,
                    Address = contact.Addresses.First().Address
                });

                //var res = await _contactProvider.UpdateAsync(contactRelation);
                var contactGrain = _clusterClient.GetGrain<IContactGrain>(contactRelation.Id);

                var dto = ObjectMapper.Map<ContactIndex, ContactGrainDto>(contactRelation);
                var updateResult = await contactGrain.UpdateContactAsync(userId, dto);
                if (!updateResult.Success)
                {
                    Logger.LogError("Imputation fail, contactId:{id}", contactRelation.Id.ToString());
                    continue;
                }

                var result =
                    await contactGrain.Imputation();

                if (!result.Success)
                {
                    Logger.LogError("Imputation fail, contactId:{id}", contactRelation.Id.ToString());
                    continue;
                }

                var contactG = _clusterClient.GetGrain<IContactGrain>(contact.Id);
                var deleteResult = await contactG.DeleteContactAsync(userId);
                if (deleteResult.Success)
                {
                    await _distributedEventBus.PublishAsync(
                        ObjectMapper.Map<ContactGrainDto, ContactUpdateEto>(deleteResult.Data), false, false);
                }
                else
                {
                    Logger.LogError("delete fail, contactId:{id}", contactRelation.Id.ToString());
                }

                await _distributedEventBus.PublishAsync(
                    ObjectMapper.Map<ContactGrainDto, ContactUpdateEto>(result.Data), false, false);
            }
        }
        // 记录被删除的联系人
    }

    public Task<ContactImputationDto> GetImputationAsync()
    {
        return Task.FromResult(new ContactImputationDto());
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
    private async Task CheckAddressAsync(Guid userId, List<ContactAddressDto> addresses, string relationId)
    {
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

        var imInfo = await GetImUserAsync(address.Address);
        if (imInfo == null || imInfo.RelationId.IsNullOrWhiteSpace()) return;

        var contactInfo = await _contactProvider.GetContactByRelationIdAsync(userId, imInfo.RelationId);
        if (contactInfo != null)
        {
            throw new UserFriendlyException("This address has already been taken in other contacts");
        }
    }

    private async Task<ContactDto> GetContactDtoAsync(CreateUpdateContactDto input)
    {
        var contact = ObjectMapper.Map<CreateUpdateContactDto, ContactDto>(input);
        if (input.Addresses.Count == 0)
        {
            return contact;
        }

        var address = input.Addresses.First();

        //contact.ImInfo = await GetImInfoAsync(input.RelationId);
        contact.ImInfo = await GetImUserAsync(address.Address);
        contact.CaHolderInfo = await GetHolderInfoAsync(contact.ImInfo, input.Addresses);

        if (address.ChainName != CommonConstant.ChainName) return contact;

        //get hash has problem
        var guardians =
            await _contactProvider.GetCaHolderInfoAsync(new List<string> { address.Address },
                contact.CaHolderInfo.CaHash);

        if (guardians?.CaHolderInfo?.Count > 0)
        {
            var addressInfos = guardians.CaHolderInfo.Where(t => t.CaAddress != address.Address)
                .Select(t => new { t.CaAddress, t.ChainId });

            foreach (var info in addressInfos)
            {
                contact.Addresses.Add(new ContactAddressDto()
                {
                    Address = info.CaAddress,
                    ChainId = info.ChainId
                });
            }
        }

        return contact;
    }

    private async Task<CaHolderInfo> GetHolderInfoAsync(ImInfo imInfo, List<ContactAddressDto> addresses)
    {
        if (imInfo != null && imInfo.PortKeyId != Guid.Empty)
        {
            return await GetHolderInfoAsync(imInfo.PortKeyId);
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
        int i = 0;
        foreach (var dto in input)
        {
            if (i < 1)
            {
                result.Add(new GetNamesResultDto()
                {
                    PortkeyId = dto,
                    Name = "Wallet 01"
                });  
            }
            else
            {
                result.Add(new GetNamesResultDto()
                {
                    PortkeyId = dto,
                    Name = ""
                });  
            }

            i++;
        }

        return result;
    }
}
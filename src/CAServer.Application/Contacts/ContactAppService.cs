using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Contacts.Provider;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains;
using CAServer.Grains.Grain.Contacts;
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

    public ContactAppService(IDistributedEventBus distributedEventBus, IClusterClient clusterClient,
        IContactProvider contactProvider)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _contactProvider = contactProvider;
    }

    public async Task<ContactResultDto> CreateAsync(CreateUpdateContactDto input)
    {
        var userId = CurrentUser.GetId();
        var existed = await CheckExistAsync(userId, input.Name);
        if (existed)
        {
            throw new UserFriendlyException(ContactMessage.ExistedMessage);
        }

        var contactDto = await GetContactDtoAsync(input);
        var contactGrain = _clusterClient.GetGrain<IContactGrain>(GuidGenerator.Create());
        var result =
            await contactGrain.AddContactAsync(userId,
                ObjectMapper.Map<ContactDto, ContactGrainDto>(contactDto));

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

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
        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(t => t.Field("caHolderInfo.userId").Terms(input.UserId)));
        mustQuery.Add(q => q.Terms(t => t.Field("addresses.address").Terms(input.KeyWord)) 
                           || q.Wildcard(i => i.Field(f => f.Name).Value($"*{input.KeyWord}*")));
        
        if (input.IsAbleChat)
        {
            mustQuery.Add(q => q.Exists(t => t.Field("imInfo.relationId")));
        }

        if (input.ModificationTime != 0)
        {
            mustQuery.Add(q => 
                q.Range(r => r.Field(c => c.ModificationTime).GreaterThanOrEquals(input.ModificationTime)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));
        
        IPromise<IList<ISort>> Sort(SortDescriptor<ContactIndex> s) => s.Ascending(a => a.Name);

        var (totalCount, contactList) = 
            await _contactRepository.GetSortListAsync(Filter, sortFunc: Sort, limit: input.MaxResultCount, skip: input.SkipCount);
        
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

        if (contacts.Count <= 1)
        {
            return;
        }
        // 查询联系人地址是否是自己的，是自己的直接删除

        // 若联系人的address中input中的address的所有联系人合并为1个、删除其它联系人

        // 记录被删除的联系人
    }

    public Task<ContactImputationDto> GetImputationAsync()
    {
        return Task.FromResult(new ContactImputationDto());
    }

    public Task ReadImputationAsync()
    {
        return Task.CompletedTask;
    }
    
    private async Task<bool> CheckExistAsync(Guid userId, string name)
    {
        if (name.IsNullOrWhiteSpace()) return false;

        var contactNameGrain =
            _clusterClient.GetGrain<IContactNameGrain>(GrainIdHelper.GenerateGrainId(userId.ToString("N"), name));
        return await contactNameGrain.IsNameExist(name);
    }

    private async Task<ContactDto> GetContactDtoAsync(CreateUpdateContactDto input)
    {
        var contact = ObjectMapper.Map<CreateUpdateContactDto, ContactDto>(input);
        contact.ImInfo = await GetImInfoAsync(input.RelationId);
        contact.CaHolderInfo = await GetHolderInfoAsync(contact.ImInfo, input.Addresses);
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
        var guardiansDto = await _contactProvider.GetCaHolderInfoAsync(new List<string> { address.Address });
        var caHash = guardiansDto?.CaHolderInfo?.FirstOrDefault()?.CaHash;
        if (caHash.IsNullOrWhiteSpace()) return null;

        var caHolder = await _contactProvider.GetCaHolderAsync(caHash);
        return ObjectMapper.Map<CAHolderIndex, CaHolderInfo>(caHolder);
    }

    private async Task<ImInfo> GetImInfoAsync(string relationId)
    {
        if (relationId.IsNullOrWhiteSpace()) return null;

        // get from im
        return new ImInfo();
    }
}
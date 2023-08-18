using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains;
using CAServer.Grains.Grain.Contacts;
using Nest;
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
    private readonly INESTRepository<ContactIndex, Guid> _contactRepository;

    public ContactAppService(IDistributedEventBus distributedEventBus, IClusterClient clusterClient,
        INESTRepository<ContactIndex, Guid> contactRepository)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _contactRepository = contactRepository;
    }

    public async Task<ContactResultDto> CreateAsync(CreateUpdateContactDto input)
    {
        var userId = CurrentUser.GetId();

        var contactNameGrain =
            _clusterClient.GetGrain<IContactNameGrain>(GrainIdHelper.GenerateGrainId(userId.ToString("N"), input.Name));
        var existed = await contactNameGrain.IsNameExist(input.Name);
        if (existed)
        {
            throw new UserFriendlyException(ContactMessage.ExistedMessage);
        }

        var contactGrain = _clusterClient.GetGrain<IContactGrain>(GuidGenerator.Create());
        var result =
            await contactGrain.AddContactAsync(userId,
                ObjectMapper.Map<CreateUpdateContactDto, ContactGrainDto>(input));

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
        var contacts = await GetContactAsync(userId);

        if (contacts.Count <= 1)
        {
            return;
        }
        // 查询联系人地址是否是自己的，是自己的直接删除
        
        // 若联系人的address中input中的address的所有联系人合并为1个、删除其它联系人
        
        // 记录被删除的联系人
    }

    private async Task<List<ContactIndex>> GetContactAsync(Guid userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));
        var contact = await _contactRepository.GetListAsync(Filter);
        if (contact.Item1 <= 0)
        {
            return new List<ContactIndex>();
        }
        
        return contact.Item2;
    }
    
}
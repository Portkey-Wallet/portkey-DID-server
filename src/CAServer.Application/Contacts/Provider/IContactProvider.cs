using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Guardian.Provider;
using CAServer.Options;
using GraphQL;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.DependencyInjection;

namespace CAServer.Contacts.Provider;

public interface IContactProvider
{
    Task<List<ContactIndex>> GetContactsAsync(Guid userId);
    Task<ContactIndex> GetContactAsync(Guid userId, Guid contactUserId);
    Task<CAHolderIndex> GetCaHolderAsync(Guid userId, string caHash);

    Task<GuardiansDto> GetCaHolderInfoAsync(List<string> caAddresses, string caHash, int skipCount = 0,
        int maxResultCount = 10);

    Task<GuardiansDto> GetCaHolderInfoByAddressAsync(List<string> caAddresses, string chainId, int skipCount = 0,
        int maxResultCount = 10);

    Task<Tuple<long, List<ContactIndex>>> GetListAsync(ContactGetListDto input);
    Task<ContactIndex> GetContactByAddressAsync(Guid userId, string address);
    Task<ContactIndex> GetContactByRelationIdAsync(Guid userId, string relationId);

    Task<bool> GetImputationAsync(Guid userId);
    Task<List<ContactIndex>> GetContactByAddressesAsync(Guid userId, List<string> addresses);

    Task<List<CAHolderIndex>> GetCaHoldersAsync(List<Guid> userIds);
}

public class ContactProvider : IContactProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly INESTRepository<ContactIndex, Guid> _contactRepository;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly ChainOptions _chainOptions;


    public ContactProvider(IGraphQLHelper graphQlHelper, INESTRepository<ContactIndex, Guid> contactRepository,
        IOptions<ChainOptions> chainOptions,
        INESTRepository<CAHolderIndex, Guid> caHolderRepository)
    {
        _graphQlHelper = graphQlHelper;
        _contactRepository = contactRepository;
        _caHolderRepository = caHolderRepository;
        _chainOptions = chainOptions.Value;
    }

    public async Task<CAHolderIndex> GetCaHolderAsync(Guid userId, string caHash)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CAHolderIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CaHash).Value(caHash)));

        if (userId != Guid.Empty)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        }

        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _caHolderRepository.GetAsync(Filter);
    }

    public async Task<GuardiansDto> GetCaHolderInfoAsync(List<string> caAddresses, string caHash, int skipCount = 0,
        int maxResultCount = 10)
    {
        return await _graphQlHelper.QueryAsync<GuardiansDto>(new GraphQLRequest
        {
            Query = @"
			    query($caAddresses:[String],$caHash:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderInfo(dto: {caAddresses:$caAddresses,caHash:$caHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData},guardianList{guardians{verifierId,identifierHash,salt,isLoginGuardian,type}}}
                }",
            Variables = new
            {
                caAddresses, caHash, skipCount, maxResultCount
            }
        });
    }

    public async Task<GuardiansDto> GetCaHolderInfoByAddressAsync(List<string> caAddresses, string chainId,
        int skipCount = 0,
        int maxResultCount = 10)
    {
        return await _graphQlHelper.QueryAsync<GuardiansDto>(new GraphQLRequest
        {
            Query = @"
			    query($caAddresses:[String],$chainId:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderInfo(dto: {caAddresses:$caAddresses,chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData},guardianList{guardians{verifierId,identifierHash,salt,isLoginGuardian,type}}}
                }",
            Variables = new
            {
                caAddresses, chainId, skipCount, maxResultCount
            }
        });
    }

    public async Task<Tuple<long, List<ContactIndex>>> GetListAsync(ContactGetListDto input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(t => t.Field("userId").Terms(input.UserId)));
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

        return
            await _contactRepository.GetSortListAsync(Filter, sortFunc: Sort, limit: input.MaxResultCount,
                skip: input.SkipCount);
    }

    public async Task<ContactIndex> GetContactByAddressAsync(Guid userId, string address)
    {
        var mustQuery = GetContactQueryContainer(userId);
        mustQuery.Add(q => q.Terms(t => t.Field("addresses.address").Terms(address)));
        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));

        var contacts = await _contactRepository.GetListAsync(Filter);
        return contacts?.Item2?.FirstOrDefault();
    }

    public async Task<ContactIndex> GetContactByRelationIdAsync(Guid userId, string relationId)
    {
        var mustQuery = GetContactQueryContainer(userId);
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ImInfo.RelationId).Value(relationId)));
        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));

        var contacts = await _contactRepository.GetListAsync(Filter);
        return contacts?.Item2?.FirstOrDefault();
    }

    public async Task<bool> GetImputationAsync(Guid userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsImputation).Value(true)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));
        var contact = await _contactRepository.GetListAsync(Filter);
        if (contact.Item1 <= 0)
        {
            return false;
        }

        return true;
    }

    public async Task<List<ContactIndex>> GetContactsAsync(Guid userId)
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

    public async Task<ContactIndex> GetContactAsync(Guid userId, Guid contactUserId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CaHolderInfo.UserId).Value(contactUserId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));
        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _contactRepository.GetAsync(Filter);
    }

    private List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>> GetContactQueryContainer(Guid userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>() { };

        if (userId != Guid.Empty)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        }

        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        return mustQuery;
    }

    public async Task<List<ContactIndex>> GetContactByAddressesAsync(Guid userId, List<string> addresses)
    {
        var mustQuery = GetContactQueryContainer(userId);
        mustQuery.Add(q => q.Terms(t => t.Field("addresses.address").Terms(addresses)));
        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));

        var contacts = await _contactRepository.GetListAsync(Filter);
        if (contacts.Item1 <= 0)
        {
            return new List<ContactIndex>();
        }

        return contacts.Item2;
    }

    public async Task<List<CAHolderIndex>> GetCaHoldersAsync(List<Guid> userIds)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CAHolderIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.UserId).Terms(userIds)));

        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var holders = await _caHolderRepository.GetListAsync(Filter);
        if (holders.Item1 <= 0)
        {
            return new List<CAHolderIndex>();
        }
        return holders.Item2;
    }
}
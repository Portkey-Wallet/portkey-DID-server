using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Guardian.Provider;
using CAServer.Options;
using CAServer.Search;
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

    Task<Tuple<long, List<ContactIndex>>> GetListAsync(Guid userId, ContactGetListDto input);
    Task<ContactIndex> GetContactByAddressAsync(Guid userId, string address);
    Task<ContactIndex> GetContactByRelationIdAsync(Guid userId, string relationId);

    Task<bool> GetImputationAsync(Guid userId);
    Task<List<ContactIndex>> GetContactByAddressesAsync(Guid userId, List<string> addresses);

    Task<List<CAHolderIndex>> GetCaHoldersAsync(List<Guid> userIds);

    Task<List<ContactIndex>> GetAddedContactsAsync(Guid userId);
    Task<List<ContactIndex>> GetContactListAsync(List<string> contactUserIds, string address, Guid currentUserId);
    Task<List<ContactIndex>> GetAllContactsAsync(int skip, int limit);

    Task<List<CAHolderIndex>> GetAllCaHolderAsync(int skip, int limit);
    Task<Tuple<long, List<CAHolderIndex>>> GetAllCaHolderWithTotalAsync(int skip, int limit);
    Task<ContactIndex> GetContactByIdAsync(Guid id);
    Task<ContactIndex> GetContactByPortKeyIdAsync(Guid userId, string toString);
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
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        if (userId != Guid.Empty)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        }

        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var holder = await _caHolderRepository.GetAsync(Filter);
        if (holder == null || holder.IsDeleted) return null;

        return holder;
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

    public async Task<Tuple<long, List<ContactIndex>>> GetListAsync(Guid userId, ContactGetListDto input)
    {
        Func<SortDescriptor<ContactIndex>, IPromise<IList<ISort>>> sort = null;
        if (!string.IsNullOrEmpty(input.Sort))
        {
            var sortList = ConvertSortOrder(input.Sort);
            var sortDescriptor = new SortDescriptor<ContactIndex>();
            sortDescriptor = sortList.Aggregate(sortDescriptor,
                (current, sortType) => current.Field(new Field(sortType.SortField), sortType.SortOrder));
            sort = s => sortDescriptor;
        }

        var filter = input.Filter.IsNullOrWhiteSpace()
            ? $"userId:{userId.ToString()}"
            : $"{input.Filter} && userId:{userId.ToString()}";

        return await _contactRepository.GetListByLucenceAsync(filter, sort,
            input.MaxResultCount, input.SkipCount);
    }

    private static IEnumerable<SortType> ConvertSortOrder(string sort)
    {
        var sortList = new List<SortType>();
        foreach (var sortOrder in sort.Split(","))
        {
            var array = sortOrder.Split(" ");
            var order = SortOrder.Ascending;
            if (string.Equals(array.Last(), "asc", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(array.Last(), "ascending", StringComparison.OrdinalIgnoreCase))
            {
                order = SortOrder.Ascending;
            }
            else if (string.Equals(array.Last(), "desc", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(array.Last(), "descending", StringComparison.OrdinalIgnoreCase))
            {
                order = SortOrder.Descending;
            }

            sortList.Add(new SortType
            {
                SortField = array.First(),
                SortOrder = order
            });
        }

        return sortList;
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
        //mustQuery.Add(q => q.Terms(i => i.Field(f => f.IsDeleted).Terms(false)));

        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var holders = await _caHolderRepository.GetListAsync(Filter);
        if (holders.Item1 <= 0)
        {
            return new List<CAHolderIndex>();
        }

        return holders.Item2;
    }

    public async Task<List<ContactIndex>> GetAddedContactsAsync(Guid userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field("caHolderInfo.userId").Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));
        var contact = await _contactRepository.GetListAsync(Filter);
        if (contact.Item1 <= 0)
        {
            return new List<ContactIndex>();
        }

        return contact.Item2;
    }

    public async Task<List<ContactIndex>> GetContactListAsync(List<string> contactUserIds, string address,
        Guid currentUserId)
    {
        if ((contactUserIds == null || !contactUserIds.Any()) && address.IsNullOrWhiteSpace())
        {
            return new List<ContactIndex>();
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(currentUserId)));

        if (contactUserIds != null && contactUserIds.Any())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.CaHolderInfo.UserId).Terms(contactUserIds)));
        }

        mustQuery.Add(q => q.Terms(t => t.Field("addresses.address").Terms(address)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));
        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));
        var res = await _contactRepository.GetListAsync(Filter, limit: 50);
        return res?.Item2;
    }

    public async Task<List<ContactIndex>> GetAllContactsAsync(int skip, int limit)
    {
        var res = await _contactRepository.GetListAsync(skip: skip, limit: limit);
        return res.Item2;
    }

    public async Task<List<CAHolderIndex>> GetAllCaHolderAsync(int skip, int limit)
    {
        var res = await _caHolderRepository.GetListAsync(skip: skip, limit: limit);
        return res.Item2;
    }
    
    public async Task<Tuple<long, List<CAHolderIndex>>> GetAllCaHolderWithTotalAsync(int skip, int limit)
    {
        return await _caHolderRepository.GetListAsync(skip: skip, limit: limit);
    }

    public async Task<ContactIndex> GetContactByIdAsync(Guid id)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(k=>k.Id).Value(id)));
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));
        var res = await _contactRepository.GetAsync(Filter);
        return res;
    }

    public async Task<ContactIndex> GetContactByPortKeyIdAsync(Guid userId, string portkeyId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(k=>k.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ImInfo.PortkeyId).Value(portkeyId)));

        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));
        var res = await _contactRepository.GetAsync(Filter);
        return res;
    }
}
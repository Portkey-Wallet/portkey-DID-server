using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.AddressBook.Dtos;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Guardian.Provider;
using CAServer.Options;
using CAServer.Search;
using GraphQL;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.DependencyInjection;

namespace CAServer.AddressBook.Provider;

public interface IAddressBookProvider
{
    Task<CAHolderIndex> GetCaHolderAsync(Guid userId, string caHash);

    Task<GuardiansDto> GetGuardianInfoAsync(List<string> caAddresses, string caHash, int skipCount = 0,
        int maxResultCount = 10);

    Task<AddressBookIndex> GetContactByAddressInfoAsync(Guid userId, string network, string chainId,
        string address);

    Task<Tuple<long, List<AddressBookIndex>>> GetListAsync(Guid userId, AddressBookListRequestDto input);
}

public class AddressBookProvider : IAddressBookProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly INESTRepository<AddressBookIndex, Guid> _addressBookRepository;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly ChainOptions _chainOptions;


    public AddressBookProvider(IGraphQLHelper graphQlHelper,
        INESTRepository<AddressBookIndex, Guid> addressBookRepository,
        IOptions<ChainOptions> chainOptions,
        INESTRepository<CAHolderIndex, Guid> caHolderRepository)
    {
        _graphQlHelper = graphQlHelper;
        _addressBookRepository = addressBookRepository;
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
        var holder = await _caHolderRepository.GetAsync(Filter);
        if (holder == null || holder.IsDeleted) return null;

        return holder;
    }

    public async Task<GuardiansDto> GetGuardianInfoAsync(List<string> caAddresses, string caHash, int skipCount = 0,
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

    public async Task<AddressBookIndex> GetContactByAddressInfoAsync(Guid userId, string network, string chainId,
        string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<AddressBookIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.AddressInfo.Network).Value(network)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.AddressInfo.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.AddressInfo.Address).Value(address)));
        QueryContainer Filter(QueryContainerDescriptor<AddressBookIndex> f) => f.Bool(b => b.Must(mustQuery));
        var contacts = await _addressBookRepository.GetListAsync(Filter);
        return contacts?.Item2?.FirstOrDefault();
    }

    public async Task<Tuple<long, List<AddressBookIndex>>> GetListAsync(Guid userId, AddressBookListRequestDto input)
    {
        Func<SortDescriptor<AddressBookIndex>, IPromise<IList<ISort>>> sort = null;
        if (!string.IsNullOrEmpty(input.Sort))
        {
            var sortList = ConvertSortOrder(input.Sort);
            var sortDescriptor = new SortDescriptor<AddressBookIndex>();
            sortDescriptor = sortList.Aggregate(sortDescriptor,
                (current, sortType) => current.Field(new Field(sortType.SortField), sortType.SortOrder));
            sort = s => sortDescriptor;
        }

        var filter = input.Filter.IsNullOrWhiteSpace()
            ? $"userId:{userId.ToString()}"
            : $"{input.Filter} && userId:{userId.ToString()}";

        return await _addressBookRepository.GetListByLucenceAsync(filter, sort,
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
}
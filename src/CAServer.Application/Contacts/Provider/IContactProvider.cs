using System;
using System.Collections.Generic;
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
    Task<CAHolderIndex> GetCaHolderAsync(string caHash);

    Task<GuardiansDto> GetCaHolderInfoAsync(List<string> caAddresses, int skipCount = 0,
        int maxResultCount = 10);
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

    public async Task<CAHolderIndex> GetCaHolderAsync(string caHash)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CAHolderIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CaHash).Value(caHash)));

        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _caHolderRepository.GetAsync(Filter);
    }

    public async Task<GuardiansDto> GetCaHolderInfoAsync(List<string> caAddresses, int skipCount = 0,
        int maxResultCount = 10)
    {
        return await _graphQlHelper.QueryAsync<GuardiansDto>(new GraphQLRequest
        {
            Query = @"
			    query($caAddresses:[String],$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderInfo(dto: {caAddresses:$caAddresses,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData},guardianList{guardians{verifierId,identifierHash,salt,isLoginGuardian,type}}}
                }",
            Variables = new
            {
                caAddresses, skipCount, maxResultCount
            }
        });
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
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.AppleAuth;
using CAServer.Common;
using CAServer.Entities.Es;
using GraphQL;
using Nest;
using Volo.Abp.DependencyInjection;

namespace CAServer.CAAccount.Provider;

public interface ICAAccountProvider
{
    Task<GuardianAddedCAHolderDto> GetGuardianAddedCAHolderAsync(string loginGuardianIdentifierHash,
        int inputSkipCount, int inputMaxResultCount);

    Task<GuardianIndex> GetIdentifiersAsync(string identifierHash);
}

public class CAAccountProvider : ICAAccountProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;

    public CAAccountProvider(IGraphQLHelper graphQlHelper, INESTRepository<GuardianIndex, string> guardianRepository)
    {
        _graphQlHelper = graphQlHelper;
        _guardianRepository = guardianRepository;
    }

    public async Task<GuardianAddedCAHolderDto> GetGuardianAddedCAHolderAsync(string loginGuardianIdentifierHash,
        int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<GuardianAddedCAHolderDto>(new GraphQLRequest
        {
            Query = @"
			    query ($loginGuardianIdentifierHash:String,$skipCount:Int!,$maxResultCount:Int!){
                    guardianAddedCAHolderInfo(dto: {loginGuardianIdentifierHash:$loginGuardianIdentifierHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                    data{id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData},guardianList{guardians{verifierId,identifierHash,salt,isLoginGuardian,type}}},totalRecordCount}
                }",
            Variables = new
            {
                loginGuardianIdentifierHash, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount
            }
        });
    }

    public async Task<GuardianIndex> GetIdentifiersAsync(string identifierHash)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IdentifierHash).Value(identifierHash)));
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var guardian = await _guardianRepository.GetAsync(Filter);
        if (guardian == null || guardian.IsDeleted) return null;

        return guardian;
    }
}
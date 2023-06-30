using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Entities.Es;
using GraphQL;
using Nest;
using Volo.Abp.DependencyInjection;

namespace CAServer.Tokens.Provider;

public interface ITokenProvider
{
    Task<IndexerTokens> GetTokenInfosAsync(string chainId, string symbol, int maxResultCount);
    Task<UserTokenIndex> GetUserTokenInfoAsync(Guid userId, string chainId, string symbol);
    Task<List<UserTokenIndex>> GetUserTokenInfoListAsync(Guid userId, string chainId, string symbol);
}

public class TokenProvider : ITokenProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly INESTRepository<UserTokenIndex, Guid> _userTokenIndexRepository;

    public TokenProvider(IGraphQLHelper graphQlHelper,
        INESTRepository<UserTokenIndex, Guid> userTokenIndexRepository)
    {
        _graphQlHelper = graphQlHelper;
        _userTokenIndexRepository = userTokenIndexRepository;
    }

    public async Task<IndexerTokens> GetTokenInfosAsync(string chainId, string symbol, int maxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerTokens>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$symbol:String,$skipCount:Int!,$maxResultCount:Int!) {
                    tokenInfo(dto: {chainId:$chainId,symbol:$symbol,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        id,chainId,blockHash,blockHeight,symbol,type,tokenContractAddress,decimals,tokenName,totalSupply,issuer,isBurnable,issueChainId
                    }
                }",
            Variables = new
            {
                chainId, symbol, skipCount = 0, maxResultCount
            }
        });
    }

    public async Task<UserTokenIndex> GetUserTokenInfoAsync(Guid userId, string chainId, string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserTokenIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Token.Symbol).Value(symbol)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Token.ChainId).Value(chainId)));
        QueryContainer Filter(QueryContainerDescriptor<UserTokenIndex> f) => f.Bool(b => b.Must(mustQuery));

        return await _userTokenIndexRepository.GetAsync();
    }

    public async Task<List<UserTokenIndex>> GetUserTokenInfoListAsync(Guid userId, string chainId, string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserTokenIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Token.Symbol).Value(symbol)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Token.ChainId).Value(chainId)));
        QueryContainer Filter(QueryContainerDescriptor<UserTokenIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, userTokens) = await _userTokenIndexRepository.GetSortListAsync();
        
        return userTokens;
    }
}
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
    Task<IndexerToken> GetTokenInfoAsync(string chainId, string symbol);
    
    Task<IndexerTokens> GetTokenInfosAsync(string chainId, string symbol, string symbolKeyword, int skipCount = 0,
        int maxResultCount = 200);

    Task<UserTokenIndex> GetUserTokenInfoAsync(Guid userId, string chainId, string symbol);
    Task<List<UserTokenIndex>> GetUserTokenInfoListAsync(Guid userId, string chainId, string symbol);

    Task<IndexerTokenApproved> GetTokenApprovedAsync(string chainId, List<string> caAddresses,
        int skipCount = 0, int maxResultCount = 2000);
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

    public async Task<IndexerToken> GetTokenInfoAsync(string chainId, string symbol)
    {
        var tokens = await GetTokenInfosAsync(chainId, symbol.Trim().ToUpper(), string.Empty, 0, 1);
        return tokens.TokenInfo.IsNullOrEmpty() ? null : tokens.TokenInfo[0];
    }

    public async Task<IndexerTokens> GetTokenInfosAsync(string chainId, string symbol, string symbolKeyword,
        int skipCount = 0, int maxResultCount = 200)
    {
        return await _graphQlHelper.QueryAsync<IndexerTokens>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$symbol:String,$symbolKeyword:String,$skipCount:Int!,$maxResultCount:Int!) {
                    tokenInfo(dto: {chainId:$chainId,symbol:$symbol,symbolKeyword:$symbolKeyword,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        id,chainId,blockHash,blockHeight,symbol,type,tokenContractAddress,decimals,tokenName,totalSupply,issuer,isBurnable,issueChainId,imageUrl
                    }
                }",
            Variables = new
            {
                chainId, symbol, symbolKeyword, skipCount, maxResultCount
            }
        });
    }
    
    public async Task<IndexerTokenApproved> GetTokenApprovedAsync(string chainId, List<string> caAddresses,
        int skipCount = 0, int maxResultCount = 2000)
    {
        return await _graphQlHelper.QueryAsync<IndexerTokenApproved>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$caAddresses:[String],$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTokenApproved(dto: {chainId:$chainId,caAddresses:$caAddresses,,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        data{chainId,caAddress,spender,symbol,batchApprovedAmount,updateTime}, totalRecordCount
                    }
                }",
            Variables = new
            {
                chainId, caAddresses, skipCount, maxResultCount
            }
        });
    }

    public async Task<UserTokenIndex> GetUserTokenInfoAsync(Guid userId, string chainId, string symbol)
    {
        var filter = GetFilter(userId, chainId, symbol);
        return await _userTokenIndexRepository.GetAsync(filter);
    }

    public async Task<List<UserTokenIndex>> GetUserTokenInfoListAsync(Guid userId, string chainId, string symbol)
    {
        var filter = GetFilter(userId, chainId, symbol);
        var (totalCount, userTokens) = await _userTokenIndexRepository.GetSortListAsync(filter);

        if (totalCount == 0)
        {
            return new List<UserTokenIndex>();
        }

        return userTokens;
    }

    private Func<QueryContainerDescriptor<UserTokenIndex>, QueryContainer> GetFilter(Guid userId,
        string chainId, string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserTokenIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Token.Symbol).Value(symbol)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Token.ChainId).Value(chainId)));
        QueryContainer QueryFilter(QueryContainerDescriptor<UserTokenIndex> f) => f.Bool(b => b.Must(mustQuery));

        return QueryFilter;
    }
}
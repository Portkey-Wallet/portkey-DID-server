using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Guardian.Provider;
using CAServer.Security.Dtos;
using GraphQL;
using Nest;
using Volo.Abp.DependencyInjection;

namespace CAServer.UserSecurity.Provider;

public class UserSecurityProvider : IUserSecurityProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly INESTRepository<UserTransferLimitHistoryIndex, Guid> _userTransferLimitHistoryRepository;

    public UserSecurityProvider(IGraphQLHelper graphQlHelper,
        INESTRepository<UserTransferLimitHistoryIndex, Guid> userTransferLimitHistoryRepository)
    {
        _graphQlHelper = graphQlHelper;
        _userTransferLimitHistoryRepository = userTransferLimitHistoryRepository;
    }

    public async Task<IndexerTransferLimitList> GetTransferLimitListByCaHashAsync(string caHash)
    {
        return await _graphQlHelper.QueryAsync<IndexerTransferLimitList>(new GraphQLRequest
        {
            Query = @"
			    query($caHash:String) {
                    caHolderTransferLimit(dto: {caHash:$caHash}){
                        data{
                            chainId,
                            cAHash,
                            symbol,
                            singleLimit,
                            dailyLimit
                            },
                        totalRecordCount
                        }
                }",
            Variables = new
            {
                caHash = caHash
            }
        });
    }

    public async Task<IndexerManagerApprovedList> GetManagerApprovedListByCaHashAsync(string caHash, string spender,
        string symbol, long skip, long maxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerManagerApprovedList>(new GraphQLRequest
        {
            Query = @"
        		query($caHash:String,$spender:String,$symbol:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderManagerApproved(dto: {caHash:$caHash,spender:$spender,symbol:$symbol,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            data{
                                chainId,
                                cAHash,
                                spender,
                                symbol,
                                amount
                                },
                            totalRecordCount
                            }
                }",
            Variables = new
            {
                caHash = caHash, spender = spender, symbol = symbol, skipCount = skip,
                maxResultCount = maxResultCount
            }
        });
    }
    
    public async Task<IndexerManagerApprovedList> ListManagerApprovedInfoByCaHashAsync(string caHash, string spender,
        string symbol, long skip, long maxResultCount, long startHeight, long endHeight)
    {
        return await _graphQlHelper.QueryAsync<IndexerManagerApprovedList>(new GraphQLRequest
        {
            Query = @"
        		query($caHash:String,$spender:String,$symbol:String,$skipCount:Int!,$maxResultCount:Int!,$startHeight:Long!,$endHeight:Long!) {
                    caHolderManagerApproved(dto: {caHash:$caHash,spender:$spender,symbol:$symbol,skipCount:$skipCount,maxResultCount:$maxResultCount,startHeight:$startHeight,endHeight:$endHeight}){
                            data{
                                chainId,
                                cAHash,
                                spender,
                                symbol,
                                amount,
                                blockHeight
                                },
                            totalRecordCount
                            }
                }",
            Variables = new
            {
                caHash = caHash, spender = spender, symbol = symbol, skipCount = skip,
                maxResultCount = maxResultCount, startHeight = startHeight, endHeight = endHeight
            }
        });
    }

    public async Task<UserTransferLimitHistoryIndex> GetUserTransferLimitHistoryAsync(string caHash, string chainId,
        string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserTransferLimitHistoryIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.CaHash).Terms(caHash)));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Symbol).Terms(symbol)));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.ChainId).Terms(chainId)));

        QueryContainer Filter(QueryContainerDescriptor<UserTransferLimitHistoryIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        return await _userTransferLimitHistoryRepository.GetAsync(Filter);
    }
    
    public async Task<GuardiansDto> GetCaHolderInfoAsync(string caHash, int skipCount = 0, int maxResultCount = 10)
    {
        return await _graphQlHelper.QueryAsync<GuardiansDto>(new GraphQLRequest
        {
            Query = @"
			    query($caHash:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderInfo(dto: {caHash:$caHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData},guardianList{guardians{verifierId,identifierHash,salt,isLoginGuardian,type,transactionId}}}
                }",
            Variables = new
            {
                caHash, skipCount, maxResultCount
            }
        });
    }
}
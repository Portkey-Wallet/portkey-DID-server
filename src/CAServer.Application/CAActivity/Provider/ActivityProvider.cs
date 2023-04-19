using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ApplicationHandler;
using GraphQL;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.DependencyInjection;

namespace CAServer.CAActivity.Provider;

public class ActivityProvider : IActivityProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderIndexRepository;
    private readonly ChainOptions _chainOptions;


    public ActivityProvider(IGraphQLHelper graphQlHelper, INESTRepository<CAHolderIndex, Guid> caHolderIndexRepository,
        IOptions<ChainOptions> chainOptions)
    {
        _graphQlHelper = graphQlHelper;
        _caHolderIndexRepository = caHolderIndexRepository;
        _chainOptions = chainOptions.Value;
    }

    public async Task<IndexerTransactions> GetActivitiesAsync(List<string> addresses, string inputChainId,
        string symbolOpt, List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerTransactions>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$symbol:String,$caAddresses:[String],$methodNames:[String],$startBlockHeight:Long!,$endBlockHeight:Long!,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTransaction(dto: {chainId:$chainId,symbol:$symbol,caAddresses:$caAddresses,methodNames:$methodNames,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        data{id,chainId,blockHash,blockHeight,previousBlockHash,transactionId,methodName,tokenInfo{symbol,tokenContractAddress,decimals,totalSupply,tokenName},status,timestamp,nftInfo{symbol,totalSupply,imageUrl,decimals,tokenName},transferInfo{fromAddress,toAddress,amount,toChainId,fromChainId,fromCAAddress},fromAddress,transactionFees{symbol,amount}},totalRecordCount
                    }
                }",
            Variables = new
            {
                caAddresses = addresses, chainId = inputChainId, symbol = symbolOpt,
                methodNames = inputTransactionTypes, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount,
                startBlockHeight = 0, endBlockHeight = 0
            }
        });
    }

    public async Task<IndexerTransactions> GetActivityAsync(string inputTransactionId, string inputBlockHash)
    {
        return await _graphQlHelper.QueryAsync<IndexerTransactions>(new GraphQLRequest
        {
            Query = @"
			    query($transactionId:String,$blockHash:String,$startBlockHeight:Long!,$endBlockHeight:Long!,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTransaction(dto: {transactionId:$transactionId,blockHash:$blockHash,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        data{id,chainId,blockHash,blockHeight,previousBlockHash,transactionId,methodName,tokenInfo{symbol,tokenContractAddress,decimals,totalSupply,tokenName},status,timestamp,nftInfo{symbol,totalSupply,imageUrl,decimals,tokenName},transferInfo{fromAddress,toAddress,amount,toChainId,fromChainId,fromCAAddress},fromAddress,transactionFees{symbol,amount}},totalRecordCount
                    }
                }",
            Variables = new
            {
                transactionId = inputTransactionId, blockHash = inputBlockHash, skipCount = 0, maxResultCount = 1,
                startBlockHeight = 0, endBlockHeight = 0
            }
        });
    }

    public async Task<string> GetCaHolderNickName(Guid userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CAHolderIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));

        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var caHolder = await _caHolderIndexRepository.GetAsync(Filter);
        return caHolder?.NickName;
    }

    public async Task<IndexerSymbols> GetTokenDecimalsAsync(string symbol)
    {
        return await _graphQlHelper.QueryAsync<IndexerSymbols>(new GraphQLRequest
        {
            Query = @"
			    query($symbol:String,$skipCount:Int!,$maxResultCount:Int!) {
                    tokenInfo(dto: {symbol:$symbol,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        decimals
                    }
                }",
            Variables = new
            {
                symbol, skipCount = 0, maxResultCount = 1
            }
        });
    }
}
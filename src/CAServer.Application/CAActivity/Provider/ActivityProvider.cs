using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.LinqToElasticSearch.Provider;
using CAServer.CAActivity.Dtos;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.UserAssets;
using GraphQL;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.DependencyInjection;

namespace CAServer.CAActivity.Provider;

public class ActivityProvider : IActivityProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly ILinqRepository<CAHolderIndex, Guid> _caHolderIndexRepository;
    private readonly ChainOptions _chainOptions;


    public ActivityProvider(IGraphQLHelper graphQlHelper, ILinqRepository<CAHolderIndex, Guid> caHolderIndexRepository,
        IOptions<ChainOptions> chainOptions)
    {
        _graphQlHelper = graphQlHelper;
        _caHolderIndexRepository = caHolderIndexRepository;
        _chainOptions = chainOptions.Value;
    }


    public async Task<TransactionsDto> GetTwoCaTransactionsAsync(List<CAAddressInfo> twoCaAddresses, string symbolOpt,
        List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<TransactionsDto>(new GraphQLRequest
        {
            Query = @"
			    query ($chainId:String,$symbol:String,$caAddressInfos:[CAAddressInfo]!,$methodNames:[String],$startBlockHeight:Long!,$endBlockHeight:Long!,$skipCount:Int!,$maxResultCount:Int!){
                    twoCaHolderTransaction(dto: {chainId:$chainId,symbol:$symbol,caAddressInfos:$caAddressInfos,methodNames:$methodNames,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                    data{id,chainId,blockHash,blockHeight,previousBlockHash,transactionId,methodName,tokenInfo{symbol,tokenContractAddress,decimals,totalSupply,tokenName},status,timestamp,nftInfo{symbol,totalSupply,imageUrl,decimals,tokenName},transferInfo{fromAddress,toAddress,amount,toChainId,fromChainId,fromCAAddress},fromAddress,transactionFees{symbol,amount}},totalRecordCount}
                }",
            Variables = new
            {
                caAddressInfos = twoCaAddresses, symbol = symbolOpt,
                methodNames = inputTransactionTypes, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount,
                startBlockHeight = 0, endBlockHeight = 0
            }
        });
    }

    public async Task<IndexerTransactions> GetActivitiesAsync(List<CAAddressInfo> caAddressInfos, string inputChainId,
        string symbolOpt, List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerTransactions>(new GraphQLRequest
        {
            Query = @"
			    query ($chainId:String,$symbol:String,$caAddressInfos:[CAAddressInfo]!,$methodNames:[String],$startBlockHeight:Long!,$endBlockHeight:Long!,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTransaction(dto: {chainId:$chainId,symbol:$symbol,caAddressInfos:$caAddressInfos,methodNames:$methodNames,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        data{id,chainId,blockHash,blockHeight,previousBlockHash,transactionId,methodName,tokenInfo{symbol,tokenContractAddress,decimals,totalSupply,tokenName},status,timestamp,nftInfo{symbol,totalSupply,imageUrl,decimals,tokenName},transferInfo{fromAddress,toAddress,amount,toChainId,fromChainId,fromCAAddress},fromAddress,transactionFees{symbol,amount}},totalRecordCount
                    }
                }",
            Variables = new
            {
                caAddressInfos = caAddressInfos, chainId = inputChainId, symbol = symbolOpt,
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
        Expression<Func<CAHolderIndex, bool>> expression = p =>
            p.UserId == userId;
        var caHolder = _caHolderIndexRepository.WhereClause(expression).Skip(0).Take(1000).ToList();

        return caHolder?.FirstOrDefault()?.NickName;
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
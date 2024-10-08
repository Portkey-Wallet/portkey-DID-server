using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Guardian.Provider;
using CAServer.UserAssets;
using GraphQL;
using Nest;
using Volo.Abp.DependencyInjection;

namespace CAServer.CAActivity.Provider;

public class ActivityProvider : IActivityProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderIndexRepository;
    private readonly INESTRepository<CaHolderTransactionIndex, string> _transactionRepository;

    public ActivityProvider(IGraphQLHelper graphQlHelper, INESTRepository<CAHolderIndex, Guid> caHolderIndexRepository,
        INESTRepository<CaHolderTransactionIndex, string> transactionRepository)
    {
        _graphQlHelper = graphQlHelper;
        _caHolderIndexRepository = caHolderIndexRepository;
        _transactionRepository = transactionRepository;
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
                        data{id,chainId,blockHash,blockHeight,previousBlockHash,transactionId,methodName,tokenInfo{symbol,tokenContractAddress,decimals,totalSupply,tokenName},status,timestamp,nftInfo{symbol,totalSupply,imageUrl,decimals,tokenName},transferInfo{fromAddress,toAddress,amount,toChainId,fromChainId,fromCAAddress},fromAddress,transactionFees{symbol,amount},isManagerConsumer,
                            toContractAddress,tokenTransferInfos{tokenInfo{symbol,decimals,tokenName,tokenContractAddress},nftInfo{symbol,decimals,tokenName,collectionName,collectionSymbol,inscriptionName,imageUrl},transferInfo{amount,fromAddress,fromCAAddress,toAddress,fromChainId,toChainId}}},totalRecordCount
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
    
    public async Task<IndexerTransactions> GetActivitiesWithBlockHeightAsync(List<CAAddressInfo> caAddressInfos, string inputChainId,
        string symbolOpt, List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount, long startHeight, long endHeight)
    {
        return await _graphQlHelper.QueryAsync<IndexerTransactions>(new GraphQLRequest
        {
            Query = @"
			    query ($chainId:String,$symbol:String,$caAddressInfos:[CAAddressInfo]!,$methodNames:[String],$startBlockHeight:Long!,$endBlockHeight:Long!,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTransaction(dto: {chainId:$chainId,symbol:$symbol,caAddressInfos:$caAddressInfos,methodNames:$methodNames,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        data{id,chainId,blockHash,blockHeight,previousBlockHash,transactionId,methodName,tokenInfo{symbol,tokenContractAddress,decimals,totalSupply,tokenName},status,timestamp,nftInfo{symbol,totalSupply,imageUrl,decimals,tokenName},transferInfo{fromAddress,toAddress,amount,toChainId,fromChainId,fromCAAddress},fromAddress,transactionFees{symbol,amount},isManagerConsumer,
                            toContractAddress,tokenTransferInfos{tokenInfo{symbol,decimals,tokenName,tokenContractAddress},nftInfo{symbol,decimals,tokenName,collectionName,collectionSymbol,inscriptionName,imageUrl},transferInfo{amount,fromAddress,fromCAAddress,toAddress,fromChainId,toChainId}}},totalRecordCount
                    }
                }",
            Variables = new
            {
                caAddressInfos = caAddressInfos, chainId = inputChainId, symbol = symbolOpt,
                methodNames = inputTransactionTypes, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount,
                startBlockHeight = startHeight, endBlockHeight = endHeight
            }
        });
    }

    public async Task<IndexerTransactions> GetActivitiesAsync(string inputChainId, List<string> inputTransactionTypes, long startBlockHeight, long endBlockHeight, int maxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerTransactions>(new GraphQLRequest
        {
            Query = @"
			    query ($chainId:String,$methodNames:[String],$startBlockHeight:Long!,$endBlockHeight:Long!,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTransaction(dto: {chainId:$chainId,methodNames:$methodNames,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        data{id,chainId,blockHash,blockHeight,previousBlockHash,transactionId,methodName,tokenInfo{symbol,tokenContractAddress,decimals,totalSupply,tokenName},status,timestamp,nftInfo{symbol,totalSupply,imageUrl,decimals,tokenName},transferInfo{fromAddress,toAddress,amount,toChainId,fromChainId,fromCAAddress},fromAddress,transactionFees{symbol,amount},isManagerConsumer,
                            toContractAddress,tokenTransferInfos{tokenInfo{symbol,decimals,tokenName,tokenContractAddress},nftInfo{symbol,decimals,tokenName,collectionName,collectionSymbol,inscriptionName,imageUrl},transferInfo{amount,fromAddress,fromCAAddress,toAddress,fromChainId,toChainId}}},totalRecordCount
                    }
                }",
            Variables = new
            {
                chainId = inputChainId,
                methodNames = inputTransactionTypes, 
                startBlockHeight, endBlockHeight,
                skipCount = 0, maxResultCount,
            }
        });
    }

    public async Task<IndexerTransactions> GetActivityAsync(string inputTransactionId, string inputBlockHash,
        List<CAAddressInfo> caAddressInfos)
    {
        return await _graphQlHelper.QueryAsync<IndexerTransactions>(new GraphQLRequest
        {
            Query = @"
			    query ($transactionId:String,$caAddressInfos:[CAAddressInfo]!,$blockHash:String,$startBlockHeight:Long!,$endBlockHeight:Long!,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTransaction(dto: {transactionId:$transactionId,caAddressInfos:$caAddressInfos,blockHash:$blockHash,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        data{id,chainId,blockHash,blockHeight,previousBlockHash,transactionId,methodName,tokenInfo{symbol,tokenContractAddress,decimals,totalSupply,tokenName},status,timestamp,nftInfo{symbol,totalSupply,imageUrl,decimals,tokenName},transferInfo{fromAddress,toAddress,amount,toChainId,fromChainId,fromCAAddress},fromAddress,transactionFees{symbol,amount},isManagerConsumer,
                            toContractAddress,tokenTransferInfos{tokenInfo{symbol,decimals,tokenName,tokenContractAddress},nftInfo{symbol,decimals,tokenName,collectionName,collectionSymbol,inscriptionName,imageUrl},transferInfo{amount,fromAddress,fromCAAddress,toAddress,fromChainId,toChainId}}},totalRecordCount
                    }
                }",
            Variables = new
            {
                caAddressInfos = caAddressInfos,
                transactionId = inputTransactionId, blockHash = inputBlockHash, skipCount = 0, maxResultCount = 1,
                startBlockHeight = 0, endBlockHeight = 0
            }
        });
    }

    public async Task<string> GetCaHolderNickName(Guid userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CAHolderIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var caHolder = await _caHolderIndexRepository.GetAsync(Filter);

        if (caHolder == null || caHolder.IsDeleted) return null;

        return caHolder?.NickName;
    }

    public async Task<IndexerSymbols> GetTokenDecimalsAsync(string symbol)
    {
        return await _graphQlHelper.QueryAsync<IndexerSymbols>(new GraphQLRequest
        {
            Query = @"
			    query($symbol:String,$skipCount:Int!,$maxResultCount:Int!) {
                    tokenInfo(dto: {symbol:$symbol,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        decimals,chainId
                    }
                }",
            Variables = new
            {
                symbol, skipCount = 0, maxResultCount = 10
            }
        });
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

    public async Task<CAHolderIndex> GetCaHolderAsync(string caHash)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CAHolderIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CaHash).Value(caHash)));

        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var caHolder = await _caHolderIndexRepository.GetAsync(Filter);
        return caHolder;
    }

    public async Task<AutoReceiveTransactions> GetAutoReceiveTransactionsAsync(List<string> transferTransactionIds,
        int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<AutoReceiveTransactions>(new GraphQLRequest
        {
            Query = @"
			    query ($transferTransactionIds:[String],$skipCount:Int!,$maxResultCount:Int!){
                    autoReceiveTransaction(dto: {transferTransactionIds:$transferTransactionIds,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                    data{chainId,blockHash,blockHeight,transactionId,methodName,status,timestamp,transferInfo{fromAddress,toAddress,amount,toChainId,fromChainId,fromCAAddress,transferTransactionId},fromAddress},totalRecordCount}
                }",
            Variables = new
            {
                transferTransactionIds, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount
            }
        });
    }

    public async Task<List<CaHolderTransactionIndex>> GetNotSuccessTransactionsAsync(string caAddress,
        long startBlockHeight,
        long endBlockHeight)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CaHolderTransactionIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CaAddress).Value(caAddress)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockHeight).GreaterThanOrEquals(startBlockHeight)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockHeight).LessThanOrEquals(endBlockHeight)));

        QueryContainer Filter(QueryContainerDescriptor<CaHolderTransactionIndex> f) => f.Bool(b => b.Must(mustQuery));
        var transactions = await _transactionRepository.GetListAsync(Filter);
        return transactions.Item2 ?? new List<CaHolderTransactionIndex>();
    }

    public async Task<CaHolderTransactionIndex> GetNotSuccessTransactionAsync(string caAddress, string transactionId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CaHolderTransactionIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CaAddress).Value(caAddress)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TransactionId).Value(transactionId)));
        
        QueryContainer Filter(QueryContainerDescriptor<CaHolderTransactionIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _transactionRepository.GetAsync(Filter);
    }

    public async Task<GuardiansDto> GetCaHolderInfoAsync(string loginGuardianIdentifierHash, int skipCount = 0, int maxResultCount = 10)
    {
        return await _graphQlHelper.QueryAsync<GuardiansDto>(new GraphQLRequest
        {
            Query = @"
			    query($loginGuardianIdentifierHash:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderInfo(dto: {loginGuardianIdentifierHash:$loginGuardianIdentifierHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData},guardianList{guardians{verifierId,identifierHash,salt,isLoginGuardian,type}}}
                }",
            Variables = new
            {
                loginGuardianIdentifierHash , skipCount, maxResultCount
            }
        });
    }
}
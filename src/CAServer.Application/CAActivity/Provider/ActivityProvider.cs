using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Common;
using GraphQL;

namespace CAServer.CAActivity.Provider;

public class ActivityProvider : IActivityProvider
{
    private readonly IGraphQLHelper _graphQlHelper;

    public ActivityProvider(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }


    public async Task<IndexerTransactions> GetActivitiesAsync(List<string> addresses, string inputChainId, string symbolOpt, List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerTransactions>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$symbol:String,$caAddresses:[String],$methodNames: [String],$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTransaction(dto: {chainId:$chainId,symbol:$symbol,caAddresses:$caAddresses,methodNames:$methodNames,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        id, chainId, blockHash, blockHeight, previousBlockHash, transactionId, methodName, tokenInfo{symbol,decimals} nftInfo{url,alias,nftId} status, timestamp, transferInfo{fromAddress,toAddress,amount,toChainId,fromChainId} fromAddress, transactionFees{symbol,amount}
                    }
                }",
            Variables = new
            {
                caAddresses = addresses, chainId = inputChainId, symbol = symbolOpt, methodNames = inputTransactionTypes, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount,
            }
        });
    }

    public async Task<IndexerTransactions> GetActivityAsync(string inputTransactionId, string inputBlockHash)
    {
        return await _graphQlHelper.QueryAsync<IndexerTransactions>(new GraphQLRequest
        {
            Query = @"
			    query($transactionId:String,$blockHash:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTransaction(dto: {transactionId:$transactionId,blockHash:$blockHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        id, chainId, blockHash, blockHeight, previousBlockHash, transactionId, methodName, tokenInfo{symbol,decimals} nftInfo{url,alias,nftId} status, timestamp, transferInfo{fromAddress,toAddress,amount,toChainId,fromChainId} fromAddress, transactionFees{symbol,amount}
                    }
                }",
            Variables = new
            {
                transactionId = inputTransactionId, blockHash = inputBlockHash, skipCount = 0, maxResultCount = 1,
            }
        });
    }
}
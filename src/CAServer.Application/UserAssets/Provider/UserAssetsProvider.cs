using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Common;
using GraphQL;

namespace CAServer.UserAssets.Provider;

public class UserAssetsProvider : IUserAssetsProvider
{
    private readonly IGraphQLHelper _graphQlHelper;

    public UserAssetsProvider(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }


    public async Task<IndexerTokenBalance> GetTokenAsync(List<string> userCaAddresses, int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerTokenBalance>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$symbol:String,$caAddresses:[String],$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTokenBalanceInfo(dto: {chainId:$chainId,symbol:$symbol,caAddresses:$caAddresses,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        chainId, tokenInfo{symbol,decimals}, balance}
                }",
            Variables = new
            {
                caAddresses = userCaAddresses, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount,
            }
        });
    }

    public async Task<IndexerNFTProtocol> GetNFTProtocolsAsync(List<string> userCaAddresses, int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerNFTProtocol>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$symbol:String,$caAddresses:[String],$skipCount:Int!,$maxResultCount:Int!) {
                    userNFTProtocolInfo(dto: {chainId:$chainId,symbol:$symbol,caAddresses:$caAddresses,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        chainId, tokenIds, nftProtocolInfo{symbol,nftType,protocolName,supply,totalSupply,issueChainId,imageUrl}}
                }",
            Variables = new
            {
                caAddresses = userCaAddresses, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount,
            }
        });
    }

    public async Task<IndexerNftInfo> GetNftInfosAsync(List<string> userCaAddresses, string symbolOpt, int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerNftInfo>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$symbol:String,$caAddresses:[String],$skipCount:Int!,$maxResultCount:Int!) {
                    userNFTInfo(dto: {chainId:$chainId,symbol:$symbol,caAddresses:$caAddresses,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        chainId, balance, nftInfo{symbol,tokenId,alias,imageUrl}}
                }",
            Variables = new
            {
                caAddresses = userCaAddresses, symbol = symbolOpt, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount,
            }
        });
    }

    public async Task<IndexerRecentTransactionUsers> GetRecentTransactionUsersAsync(List<string> userCaAddresses, int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerRecentTransactionUsers>(new GraphQLRequest
        {
            Query = @"
			    query($caAddresses:[String],$skipCount:Int!,$maxResultCount:Int!) {
                    userNFTInfo(dto: {caAddresses:$caAddresses,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        chainId, caAddress, transactionTime}
                }",
            Variables = new
            {
                caAddresses = userCaAddresses, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount,
            }
        });
    }

    public async Task<IndexerUserAssets> SearchUserAssetsAsync(List<string> userCaAddresses, string keyWord, int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerUserAssets>(new GraphQLRequest
        {
            Query = @"
			    query($searchWord:String,$caAddresses:[String],$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderSearchTokenNFT(dto: {searchWord:$searchWord,caAddresses:$caAddresses,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        chainId,balance,caAddress,tokenInfo{symbol,tokenContractAddress,decimals},nftInfo{ protocolName, symbol, tokenId, nftContractAddress}}
                }",
            Variables = new
            {
                caAddresses = userCaAddresses, searchWord = keyWord, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount
            }
        });
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Entities.Es;
using CAServer.UserAssets.Dtos;

namespace CAServer.UserAssets.Provider;

public interface IUserAssetsProvider
{
    Task<IndexerTokenInfos> GetUserTokenInfoAsync(List<CAAddressInfo> caAddressInfos, string symbol, int inputSkipCount,
        int inputMaxResultCount);

    Task<IndexerChainIds> GetUserChainIdsAsync(List<string> userCaAddresses);

    Task<IndexerNftCollectionInfos> GetUserNftCollectionInfoAsync(List<CAAddressInfo> caAddressInfos,
        int inputSkipCount,
        int inputMaxResultCount);

    Task<IndexerNftInfos> GetUserNftInfoAsync(List<CAAddressInfo> caAddressInfos, string symbol, int inputSkipCount,
        int inputMaxResultCount);

    Task<IndexerNftItemInfos> GetNftItemTraitsInfoAsync(GetNftItemInfosDto getNftItemInfosDto, int inputSkipCount,
        int inputMaxResultCount);

    Task<IndexerNftItemInfos> GetNftItemInfosAsync(GetNftItemInfosDto getNftItemInfosDto, int inputSkipCount,
        int inputMaxResultCount);

    Task<IndexerRecentTransactionUsers> GetRecentTransactionUsersAsync(List<CAAddressInfo> caAddressInfos,
        int inputSkipCount,
        int inputMaxResultCount);

    Task<IndexerSearchTokenNfts> SearchUserAssetsAsync(List<CAAddressInfo> caAddressInfos, string keyword,
        int inputSkipCount,
        int inputMaxResultCount);

    Task<List<UserTokenIndex>> GetUserDefaultTokenSymbolAsync(Guid userId);
    Task<List<UserTokenIndex>> GetUserIsDisplayTokenSymbolAsync(Guid userId);
    Task<List<(string, string)>> GetUserNotDisplayTokenAsync(Guid userId);
    Task<CAHolderIndex> GetCaHolderIndexAsync(Guid userId);

    Task<CAHolderInfo> GetCaHolderManagerInfoAsync(List<string> userCaAddresses);

    Task<CAHolderIndex> GetCaHolderIndexByCahashAsync(string caHash);

    Task<IndexerNftInfos> GetUserNftInfoBySymbolAsync(List<CAAddressInfo> caAddressInfos, string symbol,
        int inputSkipCount, int inputMaxResultCount);

    Task<IndexerNftItemWithTraitsInfos> GetNftItemWithTraitsInfos(string symbol, string collectionSymbolsymbol,
        int skipCount, int maxResultCount);
}
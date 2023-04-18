using System.Collections.Generic;
using System.Threading.Tasks;

namespace CAServer.UserAssets.Provider;

public interface IUserAssetsProvider
{
    Task<IndexerTokenBalance> GetTokenAsync(List<string> userCaAddresses, int inputSkipCount, int inputMaxResultCount);
    Task<IndexerNFTProtocol> GetNFTProtocolsAsync(List<string> userCaAddresses, int inputSkipCount, int inputMaxResultCount);
    Task<IndexerNftInfo> GetNftInfosAsync(List<string> userCaAddresses, string symbolOpt, int inputSkipCount, int inputMaxResultCount);
    Task<IndexerRecentTransactionUsers> GetRecentTransactionUsersAsync(List<string> userCaAddresses, int inputSkipCount, int inputMaxResultCount);
    Task<IndexerUserAssets> SearchUserAssetsAsync(List<string> userCaAddresses, string keyWord, int inputSkipCount, int inputMaxResultCount);
}
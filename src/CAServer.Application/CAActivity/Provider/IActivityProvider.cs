using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.UserAssets;

namespace CAServer.CAActivity.Provider;

public interface IActivityProvider
{
    Task<TransactionsDto> GetTwoCaTransactionsAsync(string fromAddress, string toAddress,
        string fromChainId, string toChainId, string symbolOpt, int inputSkipCount, int inputMaxResultCount);

    Task<IndexerTransactions> GetActivitiesAsync(List<CAAddressInfo> addressInfos, string inputChainId,
        string symbolOpt, List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount);

    Task<IndexerTransactions> GetActivityAsync(string inputTransactionId, string inputBlockHash);

    Task<string> GetCaHolderNickName(Guid userId);

    Task<IndexerSymbols> GetTokenDecimalsAsync(string symbol);
}
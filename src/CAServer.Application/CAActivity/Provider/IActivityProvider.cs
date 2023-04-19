using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Grains.Grain.ApplicationHandler;

namespace CAServer.CAActivity.Provider;

public interface IActivityProvider
{
    Task<IndexerTransactions> GetActivitiesAsync(List<string> addresses, string inputChainId, string symbolOpt,
        List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount);

    Task<IndexerTransactions> GetActivityAsync(string inputTransactionId, string inputBlockHash);

    Task<string> GetCaHolderNickName(Guid userId);

    Task<IndexerSymbols> GetTokenDecimalsAsync(string symbol);
}
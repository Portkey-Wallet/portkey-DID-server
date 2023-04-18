using System.Collections.Generic;
using System.Threading.Tasks;

namespace CAServer.CAActivity.Provider;

public interface IActivityProvider 
{
    Task<IndexerTransactions> GetActivitiesAsync(List<string> addresses, string inputChainId, string symbolOpt,List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount);
    Task<IndexerTransactions> GetActivityAsync(string inputTransactionId, string inputBlockHash);
}
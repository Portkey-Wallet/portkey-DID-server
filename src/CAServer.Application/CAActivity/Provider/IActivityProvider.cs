using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Guardian.Provider;
using CAServer.UserAssets;

namespace CAServer.CAActivity.Provider;

public interface IActivityProvider
{
    Task<TransactionsDto> GetTwoCaTransactionsAsync(List<CAAddressInfo> twoCaAddresses, string symbolOpt,
        List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount);

    Task<IndexerTransactions> GetActivitiesAsync(List<CAAddressInfo> addressInfos, string inputChainId,
        string symbolOpt, List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount);

    Task<IndexerTransactions> GetActivityAsync(string inputTransactionId, string inputBlockHash, List<CAAddressInfo> addressInfos);

    Task<string> GetCaHolderNickName(Guid userId);

    Task<IndexerSymbols> GetTokenDecimalsAsync(string symbol);

    Task<CAHolderIndex> GetCaHolderAsync(string caHash);

    Task<GuardiansDto> GetCaHolderInfoAsync(List<string> caAddresses, string caHash, int skipCount = 0,
        int maxResultCount = 10);
}
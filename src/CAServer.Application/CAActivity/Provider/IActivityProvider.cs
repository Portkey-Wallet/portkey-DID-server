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

    Task<IndexerTransactions> GetActivitiesWithBlockHeightAsync(List<CAAddressInfo> caAddressInfos, string inputChainId,
        string symbolOpt, List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount,
        long startHeight, long endHeight);

    Task<IndexerTransactions> GetActivitiesAsync(string inputChainId, List<string> inputTransactionTypes, long startBlockHeight, long endBlockHeight, int maxResultCount);
    
    Task<IndexerTransactions> GetActivityAsync(string inputTransactionId, string inputBlockHash,
        List<CAAddressInfo> addressInfos);

    Task<string> GetCaHolderNickName(Guid userId);

    Task<IndexerSymbols> GetTokenDecimalsAsync(string symbol);

    Task<CAHolderIndex> GetCaHolderAsync(string caHash);

    Task<GuardiansDto> GetCaHolderInfoAsync(List<string> caAddresses, string caHash, int skipCount = 0,
        int maxResultCount = 10);

    Task<AutoReceiveTransactions> GetAutoReceiveTransactionsAsync(List<string> transferTransactionIds,
        int inputSkipCount = 0, int inputMaxResultCount = 10);

    Task<List<CaHolderTransactionIndex>> GetNotSuccessTransactionsAsync(string caAddress, long startBlockHeight,
        long endBlockHeight);

    Task<CaHolderTransactionIndex> GetNotSuccessTransactionAsync(string caAddress, string transactionId);
    
    Task<GuardiansDto> GetCaHolderInfoAsync(string identifierHash, int skipCount = 0,
        int maxResultCount = 10);
}
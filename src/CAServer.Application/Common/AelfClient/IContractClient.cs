using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ChainOptions = CAServer.Options.ChainOptions;

namespace CAServer.Common.AelfClient;

public interface IContractClient
{
    Task<Transaction> GenerateTransactionAsync(
        string from,
        string to,
        string methodName,
        IMessage input);

    Task<BlockDto> GetBlockByHeightAsync(long blockHeight, bool includeTransactions = false);

    Task<SendTransactionOutput> SendTransactionAsync(SendTransactionInput input);

    Task<TransactionResultDto> GetTransactionResultAsync(string transactionId);
    Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input);
}


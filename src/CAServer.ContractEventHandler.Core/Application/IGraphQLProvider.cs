using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CAServer.CAActivity.Provider;
using CAServer.Grains.Grain.ApplicationHandler;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IGraphQLProvider
{
    public Task<long> GetIndexBlockHeightAsync(string chainId);
    public Task<long> GetLastEndHeightAsync(string chainId, string type);
    public Task SetLastEndHeightAsync(string chainId, string type, long height);

    public Task<List<QueryEventDto>> GetLoginGuardianAccountTransactionInfosAsync(
        string chainId, long startBlockHeight, long endBlockHeight);

    public Task<List<QueryEventDto>> GetManagerTransactionInfosAsync(string chainId,
        long startBlockHeight, long endBlockHeight);

    Task<IndexerTransactions> GetToReceiveTransactionsAsync(string chainId, long startHeight,
        long endHeight);

    Task<IndexerTransaction> GetReceiveTransactionAsync(string chainId, string transferTxId, long endHeight);

}

public class GraphQLProvider : IGraphQLProvider, ISingletonDependency
{
    private readonly GraphQLOptions _graphQLOptions;
    private readonly GraphQLHttpClient _graphQLClient;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<ContractProvider> _logger;

    public GraphQLProvider(ILogger<ContractProvider> logger, IClusterClient clusterClient,
        IOptions<GraphQLOptions> graphQLOptions)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _graphQLOptions = graphQLOptions.Value;
        _graphQLClient = new GraphQLHttpClient(_graphQLOptions.GraphQLConnection, new NewtonsoftJsonSerializer());
    }

    public async Task<long> GetIndexBlockHeightAsync(string chainId)
    {
        var req = new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$filterType:BlockFilterType!) {
                    syncState(dto: {chainId:$chainId,filterType:$filterType}){
                        confirmedBlockHeight}
                    }",
            Variables = new
            {
                chainId,
                filterType = BlockFilterType.LOG_EVENT
            }
        };
        var graphQLResponse = await _graphQLClient.SendQueryAsync<ConfirmedBlockHeightRecord>(req);
        JsonSerializer.Serialize(graphQLResponse, new JsonSerializerOptions { WriteIndented = true });
        if (graphQLResponse.Errors is { Length: > 0 })
        {
            _logger.LogError("GetIndexBlockHeight on chain {id} err: {error}", chainId, graphQLResponse.Errors);
            return 0;
        }

        return graphQLResponse.Data.SyncState.ConfirmedBlockHeight;
    }

    public async Task<long> GetLastEndHeightAsync(string chainId, string type)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(type + chainId);
            return await grain.GetStateAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetIndexBlockHeight on chain {id} error", chainId);
            return ContractAppServiceConstant.LongError;
        }
    }

    public async Task SetLastEndHeightAsync(string chainId, string type, long height)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(type +
                                                                              chainId);
            await grain.SetStateAsync(height);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SetIndexBlockHeight on chain {id} error", chainId);
        }
    }

    public async Task<List<QueryEventDto>> GetLoginGuardianAccountTransactionInfosAsync(
        string chainId, long startBlockHeight, long endBlockHeight)
    {
        if (startBlockHeight >= endBlockHeight)
        {
            _logger.LogInformation("EndBlockHeight should be higher than StartBlockHeight");
            return new List<QueryEventDto>();
        }

        var req = new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!) {
                    loginGuardianAccountChangeRecordInfo(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight}){
                        id, caHash, caAddress, changeType, loginGuardianAccount{value}, blockHeight}
                    }",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight
            }
        };

        var graphQLResponse = await _graphQLClient.SendQueryAsync<LoginGuardianAccountChangeRecords>(req);
        JsonSerializer.Serialize(graphQLResponse, new JsonSerializerOptions { WriteIndented = true });
        if (graphQLResponse.Errors is { Length: > 0 })
        {
            _logger.LogError("GetManagerTransactionInfos on chain {id} err: {error}", chainId, graphQLResponse.Errors);
            return null;
        }

        if (graphQLResponse.Data.LoginGuardianAccountChangeRecordInfo.IsNullOrEmpty())
        {
            return new List<QueryEventDto>();
        }

        var result = new List<QueryEventDto>();
        foreach (var record in graphQLResponse.Data.LoginGuardianAccountChangeRecordInfo)
        {
            result.Add(new QueryEventDto
            {
                BlockHeight = record.BlockHeight,
                CaHash = record.CaHash,
                ChangeType = record.ChangeType,
                Value = record.LoginGuardianAccount.Value
            });
        }

        return result;
    }

    public async Task<List<QueryEventDto>> GetManagerTransactionInfosAsync(string chainId,
        long startBlockHeight, long endBlockHeight)
    {
        if (startBlockHeight >= endBlockHeight)
        {
            _logger.LogError("EndBlockHeight should be higher than StartBlockHeight");
            return new List<QueryEventDto>();
        }

        var req = new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!) {
                    caHolderManagerChangeRecordInfo(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight}){
                        caAddress, caHash, manager, changeType, blockHeight}
                    }",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight
            }
        };

        var graphQLResponse = await _graphQLClient.SendQueryAsync<CAHolderManagerChangeRecords>(req);
        JsonSerializer.Serialize(graphQLResponse, new JsonSerializerOptions { WriteIndented = true });
        if (graphQLResponse.Errors is { Length: > 0 })
        {
            _logger.LogError("GetManagerTransactionInfos on chain {id} err: {error}", chainId, graphQLResponse.Errors);
            return null;
        }

        if (graphQLResponse.Data.CaHolderManagerChangeRecordInfo.IsNullOrEmpty())
        {
            return new List<QueryEventDto>();
        }

        var result = new List<QueryEventDto>();
        foreach (var record in graphQLResponse.Data.CaHolderManagerChangeRecordInfo)
        {
            result.Add(new QueryEventDto
            {
                BlockHeight = record.BlockHeight,
                CaHash = record.CaHash,
                ChangeType = record.ChangeType,
                Value = record.Manager
            });
        }

        return result;
    }
    
    public async Task<IndexerTransactions> GetToReceiveTransactionsAsync(string chainId, long startHeight,
        long endHeight)
    {
        var graphQLResponse = await _graphQLClient.SendQueryAsync<IndexerTransactions>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!,$methodNames: [String],$skipCount:Int!,$maxResultCount:Int!){
            caHolderTransaction(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight, methodNames:$methodNames,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                totalRecordCount,
                data{
                    blockHash,
                    blockHeight,
                    transactionId,
                    methodName,
                    transferInfo{
                        fromChainId,
                        toChainId
                    }
                }
            }
        }",
            Variables = new
            {
                chainId = chainId, 
                startBlockHeight = startHeight, 
                endBlockHeight = endHeight,
                methodNames = new List<string> { "CrossChainTransfer" }, 
                skipCount = 0, 
                maxResultCount = 10000
            }
        });

        return graphQLResponse.Data;
    }
    
    public async Task<IndexerTransaction> GetReceiveTransactionAsync(string chainId, string transferTxId, long endHeight)
    {
        // var blockHeight = await GetIndexBlockHeightAsync(chainId);
        // var endHeight = blockHeight - _indexOptions.IndexSafe;
        
        var txs = await _graphQLClient.SendQueryAsync<IndexerTransactions>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!,$methodNames: [String],$transferTransactionId:String,$skipCount:Int!,$maxResultCount:Int!){
            caHolderTransaction(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight, methodNames:$methodNames,transferTransactionId:$transferTransactionId,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                totalRecordCount,
                data{
                    id
                }
            }
        }",
            Variables = new
            {
                chainId = chainId, 
                startBlockHeight = 0, 
                endBlockHeight = endHeight,
                methodNames = new List<string> { "CrossChainReceiveToken" }, 
                transferTransactionId = transferTxId,
                skipCount = 0, 
                maxResultCount = 10000
            }
        });

        return txs.Data.CaHolderTransaction.Data.FirstOrDefault();
    }
}
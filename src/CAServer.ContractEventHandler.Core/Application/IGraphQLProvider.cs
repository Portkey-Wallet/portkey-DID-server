using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
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
    public Task<long> GetLastEndHeightAsync(string key);
    public Task SetLastEndHeightAsync(string chainId, string type, long height);

    public Task<List<QueryEventDto>> GetLoginGuardianAccountTransactionInfosAsync(
        string chainId, long startBlockHeight, long endBlockHeight);

    public Task<List<QueryEventDto>> GetManagerTransactionInfosAsync(string chainId,
        long startBlockHeight, long endBlockHeight);
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
            _logger.LogError("GetIndexBlockHeight err: {error}", graphQLResponse.Errors);
            return 0;
        }

        return graphQLResponse.Data.SyncState.ConfirmedBlockHeight;
    }

    public async Task<long> GetLastEndHeightAsync(string key)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(key);
            return await grain.GetStateAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetIndexBlockHeight error");
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
            _logger.LogError(e, "SetIndexBlockHeight error");
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
            _logger.LogError("GetManagerTransactionInfos err: {error}", graphQLResponse.Errors);
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
            _logger.LogError("GetManagerTransactionInfos err: {error}", graphQLResponse.Errors);
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
}
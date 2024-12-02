using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Http.Dtos;
using CAServer.Options;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer;

public interface IGraphQLProvider
{
    Task<long> GetIndexBlockHeightAsync(string chainId);
    Task<long> GetLastEndHeightAsync(string chainId, string type);
    Task SetLastEndHeightAsync(string chainId, string type, long height);

    Task<List<QueryEventDto>> GetLoginGuardianTransactionInfosAsync(
        string chainId, long startBlockHeight, long endBlockHeight);

    Task<List<QueryEventDto>> GetManagerTransactionInfosAsync(string chainId,
        long startBlockHeight, long endBlockHeight);

    Task<CaHolderTransactionInfos> GetToReceiveTransactionsAsync(string chainId, long startHeight,
        long endHeight);

    Task<IndexerTransaction> GetReceiveTransactionAsync(string chainId, string transferTxId, long endHeight);

    Task<List<QueryEventDto>> GetGuardianTransactionInfosAsync(string chainId,
        long startBlockHeight, long endBlockHeight);

    Task<CaHolderQueryDto> GetCaHolderInfoAsync(string caHash, int skipCount = 0, int maxResultCount = 1);
}

public class GraphQLProvider : IGraphQLProvider, ISingletonDependency
{
    private readonly GraphQLOptions _graphQLOptions;
    private readonly GraphQLHttpClient _graphQLClient;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<GraphQLProvider> _logger;
    private readonly IHttpClientService _httpClientService;

    public GraphQLProvider(ILogger<GraphQLProvider> logger, IClusterClient clusterClient,
        IOptionsSnapshot<GraphQLOptions> graphQLOptions, IHttpClientService httpClientService)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _httpClientService = httpClientService;
        _graphQLOptions = graphQLOptions.Value;
        _graphQLClient = new GraphQLHttpClient(_graphQLOptions.Configuration, new NewtonsoftJsonSerializer());
    }

    public async Task<long> GetIndexBlockHeightAsync(string chainId)
    {
        try
        {
            var url = _graphQLOptions.Configuration.Replace(CommonConstant.ReplaceUri, CommonConstant.SyncStateUri);
            var blockHeightInfo = await _httpClientService.GetAsync<SyncStateDto>(url);
            var blockHeightItem = blockHeightInfo?.CurrentVersion?.Items?.FirstOrDefault(t => t.ChainId == chainId);
            if (blockHeightItem == null)
            {
                throw new UserFriendlyException("[GetIndexBlockHeightAsync] data empty.");
            }

            return blockHeightItem.LongestChainHeight;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[GetIndexBlockHeightAsync] on chain {chainId} error", chainId);
            return ContractAppServiceConstant.LongEmpty;
        }
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
            return ContractAppServiceConstant.LongEmpty;
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

    public async Task<List<QueryEventDto>> GetLoginGuardianTransactionInfosAsync(
        string chainId, long startBlockHeight, long endBlockHeight)
    {
        if (startBlockHeight >= endBlockHeight)
        {
            _logger.LogInformation("EndBlockHeight should be higher than StartBlockHeight");
            return new List<QueryEventDto>();
        }

        var graphQLResponse = await _graphQLClient.SendQueryAsync<LoginGuardianChangeRecords>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!) {
                    loginGuardianChangeRecordInfo(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight}){
                        id, caHash, caAddress, changeType, manager, loginGuardian{identifierHash}, blockHeight, blockHash}
                    }",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight
            }
        });

        if (graphQLResponse.Data.LoginGuardianChangeRecordInfo.IsNullOrEmpty())
        {
            return new List<QueryEventDto>();
        }

        var result = new List<QueryEventDto>();
        foreach (var record in graphQLResponse.Data.LoginGuardianChangeRecordInfo)
        {
            result.Add(new QueryEventDto
            {
                CaHash = record.CaHash,
                ChangeType = record.ChangeType,
                Manager = record.Manager,
                BlockHeight = record.BlockHeight,
                BlockHash = record.BlockHash,
                NotLoginGuardian = record.ChangeType == QueryLoginGuardianType.LoginGuardianUnbound
                    ? record.LoginGuardian.IdentifierHash
                    : null
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

        var graphQLResponse = await _graphQLClient.SendQueryAsync<CAHolderManagerChangeRecords>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!) {
                    caHolderManagerChangeRecordInfo(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight}){
                        caAddress, caHash, manager, changeType, blockHeight, blockHash}
                    }",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight
            }
        });

        if (graphQLResponse.Data.CaHolderManagerChangeRecordInfo.IsNullOrEmpty())
        {
            return new List<QueryEventDto>();
        }

        var result = new List<QueryEventDto>();
        foreach (var record in graphQLResponse.Data.CaHolderManagerChangeRecordInfo)
        {
            result.Add(new QueryEventDto
            {
                CaHash = record.CaHash,
                ChangeType = record.ChangeType,
                Manager = record.Manager,
                BlockHeight = record.BlockHeight,
                BlockHash = record.BlockHash
            });
        }

        return result;
    }

    public async Task<CaHolderTransactionInfos> GetToReceiveTransactionsAsync(string chainId, long startHeight,
        long endHeight)
    {
        var graphQLResponse = await _graphQLClient.SendQueryAsync<CaHolderTransactionInfos>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!,$methodNames: [String],$skipCount:Int!,$maxResultCount:Int!){
            caHolderTransactionInfo(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight, methodNames:$methodNames,skipCount:$skipCount,maxResultCount:$maxResultCount}){
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
                methodNames = new List<string>
                    { CommonConstant.CrossChainTransferMethodName, CommonConstant.InlineCrossChainTransferMethodName },
                skipCount = 0,
                maxResultCount = 10000
            }
        });

        return graphQLResponse.Data;
    }

    public async Task<IndexerTransaction> GetReceiveTransactionAsync(string chainId, string transferTxId,
        long endHeight)
    {
        // var blockHeight = await GetIndexBlockHeightAsync(chainId);
        // var endHeight = blockHeight - _indexOptions.IndexSafe;

        var txs = await _graphQLClient.SendQueryAsync<CaHolderTransactionInfos>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!,$methodNames: [String],$transferTransactionId:String,$skipCount:Int!,$maxResultCount:Int!){
            caHolderTransactionInfo(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight, methodNames:$methodNames,transferTransactionId:$transferTransactionId,skipCount:$skipCount,maxResultCount:$maxResultCount}){
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

        return txs.Data.CaHolderTransactionInfo.Data.FirstOrDefault();
    }

    public async Task<List<QueryEventDto>> GetGuardianTransactionInfosAsync(string chainId, long startBlockHeight,
        long endBlockHeight)
    {
        try
        {
            if (startBlockHeight >= endBlockHeight)
            {
                _logger.LogError("EndBlockHeight should be higher than StartBlockHeight");
                return new List<QueryEventDto>();
            }

            var graphQLResponse = await _graphQLClient.SendQueryAsync<GuardianChangeRecords>(new GraphQLRequest
            {
                Query = @"
			    query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!) {
                    guardianChangeRecordInfo(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight}){
                        caAddress, caHash, changeType, blockHeight, blockHash}
                    }",
                Variables = new
                {
                    chainId,
                    startBlockHeight,
                    endBlockHeight
                }
            });

            if (graphQLResponse.Data.GuardianChangeRecordInfo.IsNullOrEmpty())
            {
                return new List<QueryEventDto>();
            }

            var result = new List<QueryEventDto>();
            foreach (var record in graphQLResponse.Data.GuardianChangeRecordInfo)
            {
                result.Add(new QueryEventDto
                {
                    CaHash = record.CaHash,
                    ChangeType = record.ChangeType,
                    BlockHeight = record.BlockHeight,
                    BlockHash = record.BlockHash
                });
            }

            return result;
        }
        catch (Exception e)
        {
            return new List<QueryEventDto>();
        }
    }

    public async Task<CaHolderQueryDto> GetCaHolderInfoAsync(string caHash, int skipCount = 0, int maxResultCount = 1)
    {
        var response = await _graphQLClient.SendQueryAsync<CaHolderQueryDto>(new GraphQLRequest
        {
            Query = @"
			    query($caHash:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderInfo(dto: {caHash:$caHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            chainId,caHash,caAddress,originChainId}
                }",
            Variables = new
            {
                caHash, skipCount, maxResultCount
            }
        });

        return response?.Data;
    }
}
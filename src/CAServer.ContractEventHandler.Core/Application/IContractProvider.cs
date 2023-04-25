using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using CAServer.Grains;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.State.ApplicationHandler;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Orleans;
using Portkey.Contracts.CA;
using Volo.Abp;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IContractProvider
{
    Task<GetHolderInfoOutput> GetHolderInfoFromChainAsync(string chainId,
        Hash loginGuardian, string caHash);

    Task<int> GetChainIdAsync(string chainId);
    Task<long> GetBlockHeightAsync(string chainId);
    Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string txId);
    Task<long> GetIndexHeightFromSideChainAsync(string sideChainId);
    Task<long> GetIndexHeightFromMainChainAsync(string chainId, int sideChainId);
    Task<TransactionResultDto> CreateHolderInfoAsync(CreateHolderDto createHolderDto);
    Task<TransactionResultDto> SocialRecoveryAsync(SocialRecoveryDto socialRecoveryDto);

    Task<TransactionInfoDto> ValidateTransactionAsync(string chainId,
        GetHolderInfoOutput result, RepeatedField<string> unsetLoginGuardians);

    Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(string chainId, TransactionInfoDto transactionInfoDto);

    Task<TransactionResultDto> SyncTransactionAsync(string chainId,
        SyncHolderInfoInput syncHolderInfoInput);

    Task MainChainCheckSideChainBlockIndexAsync(string chainIdSide, long txHeight);
    Task SideChainCheckMainChainBlockIndexAsync(string sideChainId, long txHeight);

    Task<ChainStatusDto> GetChainStatusAsync(string chainId);
    Task<BlockDto> GetBlockByHeightAsync(string chainId, long height, bool includeTransactions = false);
    
    
    Task AddSyncRecordsAsync(string chainId, List<SyncRecord> records);
    Task AddFailedRecordsAsync(string chainId, List<SyncRecord> records);
    Task<List<SyncRecord>> GetSyncRecords(string chainId);
    Task<List<SyncRecord>> GetFailedRecords(string chainId);
    Task ClearRecordsAsync(string chainId);
    Task ClearFailedRecordsAsync(string chainId);
}

public class ContractProvider : IContractProvider
{
    private readonly ILogger<ContractProvider> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ChainOptions _chainOptions;
    private readonly IndexOptions _indexOptions;

    public ContractProvider(ILogger<ContractProvider> logger, IOptions<ChainOptions> chainOptions,
        IOptions<IndexOptions> indexOptions, IClusterClient clusterClient)
    {
        _logger = logger;
        _chainOptions = chainOptions.Value;
        _indexOptions = indexOptions.Value;
        _clusterClient = clusterClient;
    }

    public async Task AddSyncRecordsAsync(string chainId, List<SyncRecord> records)
    {
        try
        {
            var grain = _clusterClient.GetGrain<ISyncRecordGrain>(GrainIdHelper.GenerateGrainId("SyncRecordGrain", chainId, "0"));
            await grain.AddRecordsAsync(records);

            _logger.LogInformation("Set SyncRecords to Chain: {id} Success: {records}", chainId, JsonConvert.SerializeObject(records));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Set SyncRecords to Chain: {id} Failed, {records}", chainId, JsonConvert.SerializeObject(records));
        }
    }

    public async Task AddFailedRecordsAsync(string chainId, List<SyncRecord> records)
    {
        try
        {
            var grain = _clusterClient.GetGrain<ISyncRecordGrain>(GrainIdHelper.GenerateGrainId("SyncRecordGrain", chainId, "0"));
            await grain.AddFailedRecordsAsync(records);

            _logger.LogInformation("Set FailedRecords to Chain: {id} Success: {records}", chainId, JsonConvert.SerializeObject(records));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Set FailedRecords to Chain: {id} Failed, {records}", chainId, JsonConvert.SerializeObject(records));
        }
    }

    public async Task<List<SyncRecord>> GetSyncRecords(string chainId)
    {
        var grain = _clusterClient.GetGrain<ISyncRecordGrain>(GrainIdHelper.GenerateGrainId("SyncRecordGrain", chainId, "0"));
        return await grain.GetRecordsAsync();
    }

    public async Task<List<SyncRecord>> GetFailedRecords(string chainId)
    {
        var grain = _clusterClient.GetGrain<ISyncRecordGrain>(GrainIdHelper.GenerateGrainId("SyncRecordGrain", chainId, "0"));
        return await grain.GetFailedRecordsAsync();
    }

    public async Task ClearRecordsAsync(string chainId)
    {
        var grain = _clusterClient.GetGrain<ISyncRecordGrain>(GrainIdHelper.GenerateGrainId("SyncRecordGrain", chainId, "0"));
        await grain.ClearRecords();
    }

    public async Task ClearFailedRecordsAsync(string chainId)
    {
        var grain = _clusterClient.GetGrain<ISyncRecordGrain>(GrainIdHelper.GenerateGrainId("SyncRecordGrain", chainId, "0"));
        await grain.ClearFailedRecords();
    }

    private async Task<T> CallTransactionAsync<T>(string chainId, string methodName, IMessage param,
        bool isCrossChain) where T : IMessage<T>, new()
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();
            var ownAddress = client.GetAddressFromPrivateKey(chainInfo.PrivateKey);
            var contractAddress = isCrossChain ? chainInfo.CrossChainContractAddress : chainInfo.ContractAddress;

            var transaction =
                await client.GenerateTransactionAsync(ownAddress, contractAddress,
                    methodName, param);
            var txWithSign = client.SignTransaction(chainInfo.PrivateKey, transaction);

            var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });

            var value = new T();
            value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));

            return value;
        }
        catch (Exception e)
        {
            if (methodName != MethodName.GetHolderInfo)
            {
                _logger.LogError(e, methodName + " error: {param}", param);
            }

            return new T();
        }
    }

    public async Task<GetHolderInfoOutput> GetHolderInfoFromChainAsync(string chainId, Hash loginGuardian,
        string caHash)
    {
        var param = new GetHolderInfoInput
        {
            CaHash = null,
            LoginGuardianIdentifierHash = loginGuardian
        };

        if (caHash != null)
        {
            param.CaHash = Hash.LoadFromHex(caHash);
        }

        var output =
            await CallTransactionAsync<GetHolderInfoOutput>(chainId, MethodName.GetHolderInfo, param, false);

        if (output == null || output.CaHash.IsNullOrEmpty())
        {
            _logger.LogInformation(MethodName.GetHolderInfo + ": Empty result");
            return new GetHolderInfoOutput();
        }

        _logger.LogDebug(MethodName.GetHolderInfo + " result: {output}",
            JsonConvert.SerializeObject(output.ToString(), Formatting.Indented));

        return output;
    }

    public async Task<int> GetChainIdAsync(string chainId)
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();
            return await client.GetChainIdAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetChainId on chain: {id} error", chainId);
            return ContractAppServiceConstant.IntError;
        }
    }

    public async Task<long> GetBlockHeightAsync(string chainId)
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();
            return await client.GetBlockHeightAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetBlockHeight on chain: {id} error", chainId);
            return ContractAppServiceConstant.LongError;
        }
    }

    public async Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string txId)
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.BaseUrl);
            return await client.GetTransactionResultAsync(txId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetTransactionResult on chain {chainId} with txId: {txId} error", chainId, txId);
            return new TransactionResultDto();
        }
    }

    public async Task<long> GetIndexHeightFromSideChainAsync(string sideChainId)
    {
        var result =
            await CallTransactionAsync<Int64Value>(sideChainId, MethodName.GetParentChainHeight, new Empty(), true);
        if (result != null)
        {
            return result.Value;
        }

        _logger.LogError(MethodName.GetParentChainHeight + ": Empty result");
        return ContractAppServiceConstant.LongError;
    }

    public async Task<long> GetIndexHeightFromMainChainAsync(string chainId, int sideChainId)
    {
        var param = new Int32Value
        {
            Value = sideChainId
        };

        var result = await CallTransactionAsync<Int64Value>(chainId, MethodName.GetSideChainHeight, param, true);
        if (result != null)
        {
            return result.Value;
        }

        _logger.LogError(MethodName.GetSideChainHeight + ": Empty result");
        return ContractAppServiceConstant.LongError;
    }

    public async Task<TransactionResultDto> CreateHolderInfoAsync(CreateHolderDto createHolderDto)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());
            var result =
                await grain.CreateHolderInfoAsync(createHolderDto);

            _logger.LogInformation(
                "CreateHolderInfo to chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                createHolderDto.ChainId,
                result.TransactionId, result.BlockNumber, result.Status, result.Error);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CreateHolderInfo error: {message}",
                JsonConvert.SerializeObject(createHolderDto.ToString(), Formatting.Indented));
            return new TransactionResultDto
            {
                Status = TransactionState.Failed,
                Error = e.Message
            };
        }
    }

    public async Task<TransactionResultDto> SocialRecoveryAsync(SocialRecoveryDto socialRecoveryDto)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());
            var result = await grain.SocialRecoveryAsync(socialRecoveryDto);

            _logger.LogInformation(
                "SocialRecovery to chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                socialRecoveryDto.ChainId,
                result.TransactionId, result.BlockNumber, result.Status, result.Error);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SocialRecovery error: {message}",
                JsonConvert.SerializeObject(socialRecoveryDto.ToString(), Formatting.Indented));
            return new TransactionResultDto
            {
                Status = TransactionState.Failed,
                Error = e.Message
            };
        }
    }

    public async Task<TransactionInfoDto> ValidateTransactionAsync(string chainId,
        GetHolderInfoOutput result, RepeatedField<string> unsetLoginGuardians)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());
            var transactionDto =
                await grain.ValidateTransactionAsync(chainId, result, unsetLoginGuardians);

            _logger.LogInformation(
                "ValidateTransaction to chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                chainId,
                transactionDto.TransactionResultDto.TransactionId, transactionDto.TransactionResultDto.BlockNumber,
                transactionDto.TransactionResultDto.Status,
                transactionDto.TransactionResultDto.Error);

            return transactionDto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ValidateTransaction on chain: {id} error", chainId);
            return new TransactionInfoDto();
        }
    }

    public async Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(string chainId,
        TransactionInfoDto transactionInfoDto)
    {
        try
        {
            if (transactionInfoDto.TransactionResultDto == null || transactionInfoDto.Transaction == null)
            {
                return new SyncHolderInfoInput();
            }

            var grain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());
            var syncHolderInfoInput = await grain.GetSyncHolderInfoInputAsync(chainId, transactionInfoDto);

            _logger.LogInformation("GetSyncHolderInfoInput on chain {id} succeed", chainId);

            return syncHolderInfoInput;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetSyncHolderInfoInput on chain: {id} error: {dto}", chainId,
                JsonConvert.SerializeObject(transactionInfoDto.TransactionResultDto ?? new TransactionResultDto(),
                    Formatting.Indented));
            return new SyncHolderInfoInput();
        }
    }

    public async Task MainChainCheckSideChainBlockIndexAsync(string chainIdSide, long txHeight)
    {
        var mainHeight = long.MaxValue;
        var checkResult = false;
        var sideChainIdInInt = await GetChainIdAsync(chainIdSide);
        var time = 0;

        while (!checkResult && time < _indexOptions.IndexTimes)
        {
            var indexSideChainBlock =
                await GetIndexHeightFromMainChainAsync(ContractAppServiceConstant.MainChainId,
                    sideChainIdInInt);

            if (indexSideChainBlock < txHeight)
            {
                _logger.LogInformation(LoggerMsg.IndexBlockRecordInformation);
                await Task.Delay(_indexOptions.IndexDelay);
                time++;
                continue;
            }

            mainHeight = mainHeight == long.MaxValue
                ? await GetBlockHeightAsync(ContractAppServiceConstant.MainChainId)
                : mainHeight;

            var indexMainChainBlock = await GetIndexHeightFromSideChainAsync(chainIdSide);
            checkResult = indexMainChainBlock > mainHeight;
        }

        CheckIndexBlockHeightResult(checkResult, time);
    }

    public async Task SideChainCheckMainChainBlockIndexAsync(string sideChainId, long txHeight)
    {
        var checkResult = false;
        var time = 0;

        while (!checkResult && time < _indexOptions.IndexTimes)
        {
            var indexMainChainBlock = await GetIndexHeightFromSideChainAsync(sideChainId);

            if (indexMainChainBlock < txHeight)
            {
                _logger.LogInformation(LoggerMsg.IndexBlockRecordInformation);
                await Task.Delay(_indexOptions.IndexDelay);
                time++;
                continue;
            }

            checkResult = true;
        }

        CheckIndexBlockHeightResult(checkResult, time);
    }

    private void CheckIndexBlockHeightResult(bool result, int time)
    {
        if (!result && time == _indexOptions.IndexTimes)
        {
            _logger.LogError(LoggerMsg.IndexTimeoutError);
            throw new UserFriendlyException(LoggerMsg.IndexTimeoutError);
        }
    }

    public async Task<TransactionResultDto> SyncTransactionAsync(string chainId, SyncHolderInfoInput input)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());
            var result = await grain.SyncTransactionAsync(chainId, input);

            _logger.LogInformation(
                "SyncTransaction to chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                chainId, result.TransactionId, result.BlockNumber, result.Status, result.Error);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SyncTransaction to Chain: {id} Error: {input}", chainId,
                JsonConvert.SerializeObject(input.ToString(), Formatting.Indented));
            return new TransactionResultDto();
        }
    }

    public async Task<ChainStatusDto> GetChainStatusAsync(string chainId)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        var client = new AElfClient(chainInfo.BaseUrl);
        return await client.GetChainStatusAsync();
    }

    public async Task<BlockDto> GetBlockByHeightAsync(string chainId, long height, bool includeTransactions = false)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        var client = new AElfClient(chainInfo.BaseUrl);
        return await client.GetBlockByHeightAsync(height, includeTransactions);
    }
}
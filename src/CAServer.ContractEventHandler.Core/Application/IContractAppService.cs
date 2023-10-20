using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.Etos;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.State.ApplicationHandler;
using CAServer.Monitor;
using CAServer.Monitor.Logger;
using CAServer.UserBehavior;
using CAServer.UserBehavior.Etos;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Portkey.Contracts.CA;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IContractAppService
{
    Task CreateHolderInfoAsync(AccountRegisterCreateEto message);
    Task SocialRecoveryAsync(AccountRecoverCreateEto message);
    Task QueryAndSyncAsync();

    Task InitializeIndexAsync();
    // Task InitializeQueryRecordIndexAsync();
    // Task InitializeIndexAsync(long blockHeight);
}

public class ContractAppService : IContractAppService
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ChainOptions _chainOptions;
    private readonly IndexOptions _indexOptions;
    private readonly IGraphQLProvider _graphQLProvider;
    private readonly IContractProvider _contractProvider;
    private readonly IRecordsBucketContainer _recordsBucketContainer;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ContractAppService> _logger;
    private readonly IIndicatorLogger _indicatorLogger;
    private readonly IMonitorLogProvider _monitorLogProvider;
    private readonly IDistributedCache<string> _distributedCache;

    public ContractAppService(IDistributedEventBus distributedEventBus, IOptionsSnapshot<ChainOptions> chainOptions,
        IOptionsSnapshot<IndexOptions> indexOptions, IGraphQLProvider graphQLProvider,
        IContractProvider contractProvider, IObjectMapper objectMapper, ILogger<ContractAppService> logger,
        IRecordsBucketContainer recordsBucketContainer, IIndicatorLogger indicatorLogger,
        IMonitorLogProvider monitorLogProvider, IDistributedCache<string> distributedCache)
    {
        _distributedEventBus = distributedEventBus;
        _indexOptions = indexOptions.Value;
        _chainOptions = chainOptions.Value;
        _graphQLProvider = graphQLProvider;
        _contractProvider = contractProvider;
        _objectMapper = objectMapper;
        _logger = logger;
        _recordsBucketContainer = recordsBucketContainer;
        _indicatorLogger = indicatorLogger;
        _monitorLogProvider = monitorLogProvider;
        _distributedCache = distributedCache;
    }

    public async Task CreateHolderInfoAsync(AccountRegisterCreateEto message)
    {
        _logger.LogInformation("CreateHolder message: " + "\n{message}",
            JsonConvert.SerializeObject(message, Formatting.Indented));

        var registerResult = new CreateHolderEto
        {
            Id = message.Id,
            Context = message.Context,
            GrainId = message.GrainId
        };

        CreateHolderDto createHolderDto;

        try
        {
            createHolderDto = _objectMapper.Map<AccountRegisterCreateEto, CreateHolderDto>(message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CreateHolderInfo AutoMapper error: {message}",
                JsonConvert.SerializeObject(message, Formatting.Indented));

            registerResult.RegisterMessage = e.Message;
            registerResult.RegisterSuccess = false;

            await _distributedEventBus.PublishAsync(registerResult);
            _logger.LogInformation("Register state pushed: " + "\n{result}",
                JsonConvert.SerializeObject(registerResult, Formatting.Indented));

            return;
        }

        var checkCaHolderExists = await CheckHolderExistsOnBothChains(createHolderDto.GuardianInfo.IdentifierHash);

        if (!checkCaHolderExists)
        {
            registerResult.RegisterMessage = "LoginGuardian: " + createHolderDto.GuardianInfo.IdentifierHash +
                                             " already exists";
            registerResult.RegisterSuccess = false;

            await _distributedEventBus.PublishAsync(registerResult);

            _logger.LogInformation("Register state pushed: " + "\n{result}",
                JsonConvert.SerializeObject(registerResult, Formatting.Indented));

            return;
        }

        var resultCreateCaHolder = await _contractProvider.CreateHolderInfoAsync(createHolderDto);

        if (resultCreateCaHolder.Status != TransactionState.Mined)
        {
            registerResult.RegisterMessage = "Transaction status: " + resultCreateCaHolder.Status + ". Error: " +
                                             resultCreateCaHolder.Error;
            registerResult.RegisterSuccess = false;

            _logger.LogInformation("Register state pushed: " + "\n{result}",
                JsonConvert.SerializeObject(registerResult, Formatting.Indented));

            await _distributedEventBus.PublishAsync(registerResult);

            return;
        }

        if (!resultCreateCaHolder.Logs.Select(l => l.Name).Contains(LogEvent.CAHolderCreated))
        {
            registerResult.RegisterMessage = "Transaction status: FAILED" + ". Error: Verification failed";
            registerResult.RegisterSuccess = false;

            _logger.LogInformation("Register state pushed, id:{id}, grainId:{grainId}, message:{result}",
                registerResult.Id.ToString(), registerResult.GrainId, registerResult.RegisterMessage);

            await _distributedEventBus.PublishAsync(registerResult);

            return;
        }

        var outputGetHolderInfo =
            await _contractProvider.GetHolderInfoFromChainAsync(createHolderDto.ChainId,
                createHolderDto.GuardianInfo.IdentifierHash, null);

        if (outputGetHolderInfo.CaHash == null || outputGetHolderInfo.CaHash.Value.IsEmpty)
        {
            registerResult.RegisterMessage = "No account found";
            registerResult.RegisterSuccess = false;

            _logger.LogInformation("Register state pushed: " + "\n{result}",
                JsonConvert.SerializeObject(registerResult, Formatting.Indented));

            return;
        }

        registerResult.CaAddress = outputGetHolderInfo.CaAddress.ToBase58();
        registerResult.RegisterSuccess = true;
        registerResult.CaHash = outputGetHolderInfo.CaHash.ToHex();

        await _distributedEventBus.PublishAsync(registerResult);

        _logger.LogInformation("Register state pushed: " + "\n{result}",
            JsonConvert.SerializeObject(registerResult, Formatting.Indented));

        // ValidateAndSync can be very time consuming, so don't wait for it to finish
        _ = ValidateTransactionAndSyncAsync(createHolderDto.ChainId, outputGetHolderInfo, "", MonitorTag.Register);
    }

    public async Task SocialRecoveryAsync(AccountRecoverCreateEto message)
    {
        _logger.LogInformation("SocialRecovery message: " + "\n{message}",
            JsonConvert.SerializeObject(message, Formatting.Indented));

        var recoveryResult = new SocialRecoveryEto
        {
            Id = message.Id,
            Context = message.Context,
            GrainId = message.GrainId
        };

        SocialRecoveryDto socialRecoveryDto;

        try
        {
            socialRecoveryDto = _objectMapper.Map<AccountRecoverCreateEto, SocialRecoveryDto>(message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SocialRecovery AutoMapper error: {message}", message);

            recoveryResult.RecoveryMessage = e.Message;
            recoveryResult.RecoverySuccess = false;

            await _distributedEventBus.PublishAsync(recoveryResult);
            _logger.LogInformation("Recovery state pushed: " + "\n{result}",
                JsonConvert.SerializeObject(recoveryResult, Formatting.Indented));

            return;
        }

        var resultSocialRecovery = await _contractProvider.SocialRecoveryAsync(socialRecoveryDto);

        if (resultSocialRecovery.Status != TransactionState.Mined)
        {
            recoveryResult.RecoveryMessage = "Transaction status: " + resultSocialRecovery.Status + ". Error: " +
                                             resultSocialRecovery.Error;
            recoveryResult.RecoverySuccess = false;

            _logger.LogInformation("Recovery state pushed: " + "\n{result}",
                JsonConvert.SerializeObject(recoveryResult, Formatting.Indented));

            await _distributedEventBus.PublishAsync(recoveryResult);

            return;
        }

        if (!resultSocialRecovery.Logs.Select(l => l.Name).Contains(LogEvent.ManagerInfoSocialRecovered))
        {
            recoveryResult.RecoveryMessage = "Transaction status: FAILED" + ". Error: Verification failed";
            recoveryResult.RecoverySuccess = false;

            _logger.LogInformation("Recovery state pushed, id:{id}, grainId:{grainId}, message:{result}",
                recoveryResult.Id.ToString(), recoveryResult.GrainId, recoveryResult.RecoveryMessage);

            await _distributedEventBus.PublishAsync(recoveryResult);

            return;
        }

        var outputGetHolderInfo = await _contractProvider.GetHolderInfoFromChainAsync(socialRecoveryDto.ChainId,
            socialRecoveryDto.LoginGuardianIdentifierHash, null);

        if (outputGetHolderInfo.CaHash == null || outputGetHolderInfo.CaHash.Value.IsEmpty)
        {
            recoveryResult.RecoveryMessage = "No account found";
            recoveryResult.RecoverySuccess = false;

            _logger.LogInformation("Recovery state pushed: " + "\n{result}",
                JsonConvert.SerializeObject(recoveryResult, Formatting.Indented));

            return;
        }

        recoveryResult.RecoverySuccess = true;
        recoveryResult.CaHash = outputGetHolderInfo.CaHash.ToHex();
        recoveryResult.CaAddress = outputGetHolderInfo.CaAddress.ToBase58();

        await _distributedEventBus.PublishAsync(recoveryResult);

        _logger.LogInformation("Recovery state pushed: " + "\n{result}",
            JsonConvert.SerializeObject(recoveryResult, Formatting.Indented));

        // _logger.LogInformation("ValidateTransactionAndSyncAsync, holderInfo: {holderInfo}",
        //     JsonConvert.SerializeObject(outputGetHolderInfo));
        // ValidateAndSync can be very time consuming, so don't wait for it to finish
        _ = ValidateTransactionAndSyncAsync(socialRecoveryDto.ChainId, outputGetHolderInfo, "",
            MonitorTag.SocialRecover);
    }

    private async Task ValidateTransactionAndSyncAsync(string chainId, GetHolderInfoOutput result,
        string optionChainId, MonitorTag target)
    {
        var stopwatch = Stopwatch.StartNew();
        await ValidateTransactionAndSyncAsync(chainId, result, optionChainId);
        stopwatch.Stop();

        var duration = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
        _indicatorLogger.LogInformation(MonitorTag.ChainDataSync, target.ToString(), duration);
    }

    private async Task<bool> ValidateTransactionAndSyncAsync(string chainId, GetHolderInfoOutput result,
        string optionChainId)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        var transactionDto =
            await _contractProvider.ValidateTransactionAsync(chainId, result, null);

        var validateHeight = transactionDto.TransactionResultDto.BlockNumber;
        SyncHolderInfoInput syncHolderInfoInput;

        var syncSucceed = true;

        if (chainInfo.IsMainChain)
        {
            foreach (var sideChain in _chainOptions.ChainInfos.Values.Where(c =>
                         !c.IsMainChain && c.ChainId != optionChainId))
            {
                if (!await CheckSyncHolderVersionAsync(sideChain.ChainId, result.CaHash.ToHex(), validateHeight))
                {
                    continue;
                }

                await _contractProvider.SideChainCheckMainChainBlockIndexAsync(sideChain.ChainId, validateHeight);

                syncHolderInfoInput =
                    await _contractProvider.GetSyncHolderInfoInputAsync(chainId, new TransactionInfo
                    {
                        TransactionId = transactionDto.TransactionResultDto.TransactionId,
                        BlockNumber = transactionDto.TransactionResultDto.BlockNumber,
                        Transaction = transactionDto.Transaction.ToByteArray()
                    });

                if (syncHolderInfoInput.VerificationTransactionInfo == null)
                {
                    return false;
                }

                var resultDto = await _contractProvider.SyncTransactionAsync(sideChain.ChainId, syncHolderInfoInput);
                syncSucceed = syncSucceed && resultDto.Status == TransactionState.Mined;
                if (syncSucceed)
                {
                    await UpdateSyncHolderVersionAsync(sideChain.ChainId, result.CaHash.ToHex(), validateHeight);
                }
            }
        }
        else
        {
            if (!await CheckSyncHolderVersionAsync(ContractAppServiceConstant.MainChainId, result.CaHash.ToHex(), validateHeight))
            {
                return false;
            }
            await _contractProvider.MainChainCheckSideChainBlockIndexAsync(chainId, validateHeight);

            syncHolderInfoInput =
                await _contractProvider.GetSyncHolderInfoInputAsync(chainId, new TransactionInfo
                {
                    TransactionId = transactionDto.TransactionResultDto.TransactionId,
                    BlockNumber = transactionDto.TransactionResultDto.BlockNumber,
                    Transaction = transactionDto.Transaction.ToByteArray()
                });

            if (syncHolderInfoInput.VerificationTransactionInfo == null)
            {
                return false;
            }

            var syncResult =
                await _contractProvider.SyncTransactionAsync(ContractAppServiceConstant.MainChainId,
                    syncHolderInfoInput);

            // result = await _contractProvider.GetHolderInfoFromChainAsync(ContractAppServiceConstant.MainChainId, Hash.Empty, result.CaHash.ToHex());
            //
            // syncSucceed =
            //     await ValidateTransactionAndSyncAsync(ContractAppServiceConstant.MainChainId, result, chainId);
            syncSucceed = syncResult.Status == TransactionState.Mined;
            if (syncSucceed)
            {
                await UpdateSyncHolderVersionAsync(ContractAppServiceConstant.MainChainId, result.CaHash.ToHex(), validateHeight);
            }
        }

        return syncSucceed;
    }

    private async Task<bool> CheckHolderExistsOnBothChains(Hash loginGuardian)
    {
        var tasks = new List<Task<bool>>();
        foreach (var chainId in _chainOptions.ChainInfos.Keys)
        {
            tasks.Add(CheckHolderExists(chainId, loginGuardian));
        }

        var results = await tasks.WhenAll();

        return results.All(r => !r);
    }

    private async Task<bool> CheckHolderExists(string chainId, Hash loginGuardian)
    {
        var outputMain = await _contractProvider.GetHolderInfoFromChainAsync(chainId, loginGuardian, null);
        if (outputMain.CaHash == null || outputMain.CaHash.Value.IsEmpty)
        {
            return false;
        }

        _logger.LogInformation("LoginGuardian: {loginGuardian} on chain {id} is occupied",
            loginGuardian.ToHex(), chainId);
        return true;
    }

    public async Task QueryAndSyncAsync()
    {
        var tasks = new List<Task>();
        foreach (var chainId in _chainOptions.ChainInfos.Keys)
        {
            tasks.Add(QueryEventsAndSyncAsync(chainId));
        }

        await tasks.WhenAll();
    }

    private async Task QueryEventsAndSyncAsync(string chainId)
    {
        await QueryEventsAsync(chainId);

        await ValidateQueryEventsAsync(chainId);

        await SyncQueryEventsAsync(chainId);
    }

    private async Task SyncQueryEventsAsync(string chainId)
    {
        _logger.LogInformation("SyncQueryEvents on chain: {id} starts", chainId);

        try
        {
            var failedRecords = new List<SyncRecord>();

            var records = await _recordsBucketContainer.GetValidatedRecordsAsync(chainId);

            if (records.IsNullOrEmpty())
            {
                _logger.LogInformation("Found no record to sync on chain: {id}", chainId);
                return;
            }

            var recordsAmount = records.Count;

            _logger.LogInformation("Found {count} records to sync on chain: {id}", records.Count, chainId);

            if (chainId == ContractAppServiceConstant.MainChainId)
            {
                foreach (var info in _chainOptions.ChainInfos.Values.Where(info => !info.IsMainChain))
                {
                    var indexHeight = await _contractProvider.GetIndexHeightFromSideChainAsync(info.ChainId);

                    await _monitorLogProvider.AddHeightIndexMonitorLogAsync(chainId, indexHeight);
                    var record = records.FirstOrDefault(r => r.ValidateHeight < indexHeight);

                    while (record != null)
                    {
                        _monitorLogProvider.AddNode(record, DataSyncType.BeginSync);

                        if (!await CheckSyncHolderVersionAsync(info.ChainId, record.CaHash, record.ValidateHeight))
                        {
                            records.Remove(record);
                            record = records.FirstOrDefault(r => r.ValidateHeight < indexHeight);
                            continue;
                        }

                        var syncHolderInfoInput =
                            await _contractProvider.GetSyncHolderInfoInputAsync(chainId,
                                record.ValidateTransactionInfoDto);
                        var result = await _contractProvider.SyncTransactionAsync(info.ChainId, syncHolderInfoInput);

                        records.Remove(record);

                        if (result.Status != TransactionState.Mined)
                        {
                            _logger.LogError(
                                "{type} SyncToSide failed on chain: {id} of account: {hash}, error: {error}, data:{data}",
                                record.ChangeType, chainId, record.CaHash, result.Error,
                                JsonConvert.SerializeObject(syncHolderInfoInput));

                            record.RetryTimes++;
                            record.ValidateHeight = long.MaxValue;
                            record.ValidateTransactionInfoDto = new TransactionInfo();

                            failedRecords.Add(record);
                        }
                        else
                        {
                            await _monitorLogProvider.FinishAsync(record, info.ChainId, result.BlockNumber);
                            await _monitorLogProvider.AddMonitorLogAsync(chainId, record.BlockHeight, info.ChainId,
                                result.BlockNumber,
                                record.ChangeType);
                            _logger.LogInformation("{type} SyncToSide succeed on chain: {id} of account: {hash}",
                                record.ChangeType, chainId, record.CaHash);
                            await UpdateSyncHolderVersionAsync(info.ChainId, record.CaHash, record.ValidateHeight);
                        }

                        record = records.FirstOrDefault(r => r.ValidateHeight < indexHeight);
                    }
                }
            }
            else
            {
                var indexHeight = await _contractProvider.GetIndexHeightFromMainChainAsync(
                    ContractAppServiceConstant.MainChainId, await _contractProvider.GetChainIdAsync(chainId));

                await _monitorLogProvider.AddHeightIndexMonitorLogAsync(chainId, indexHeight);
                var record = records.FirstOrDefault(r => r.ValidateHeight < indexHeight);

                while (record != null)
                {
                    _monitorLogProvider.AddNode(record, DataSyncType.BeginSync);
                    if (!await CheckSyncHolderVersionAsync(ContractAppServiceConstant.MainChainId, record.CaHash, record.ValidateHeight))
                    {
                        continue;
                    }

                    var retryTimes = 0;
                    var mainHeight =
                        await _contractProvider.GetBlockHeightAsync(ContractAppServiceConstant.MainChainId);
                    var indexMainChainBlock = await _contractProvider.GetIndexHeightFromSideChainAsync(chainId);

                    while (indexMainChainBlock <= mainHeight && retryTimes < _indexOptions.IndexTimes)
                    {
                        await Task.Delay(_indexOptions.IndexDelay);
                        indexMainChainBlock = await _contractProvider.GetIndexHeightFromSideChainAsync(chainId);
                        retryTimes++;
                    }

                    var syncHolderInfoInput =
                        await _contractProvider.GetSyncHolderInfoInputAsync(chainId, record.ValidateTransactionInfoDto);
                    var result =
                        await _contractProvider.SyncTransactionAsync(ContractAppServiceConstant.MainChainId,
                            syncHolderInfoInput);

                    records.Remove(record);

                    if (result.Status != TransactionState.Mined)
                    {
                        _logger.LogError(
                            "{type} SyncToMain failed on chain: {id} of account: {hash}, error: {error}, data{data}",
                            record.ChangeType, chainId, record.CaHash, result.Error,
                            JsonConvert.SerializeObject(syncHolderInfoInput));

                        record.RetryTimes++;
                        record.ValidateHeight = long.MaxValue;
                        record.ValidateTransactionInfoDto = new TransactionInfo();

                        failedRecords.Add(record);
                    }
                    else
                    {
                        await _monitorLogProvider.FinishAsync(record, ContractAppServiceConstant.MainChainId,
                            result.BlockNumber);
                        await _monitorLogProvider.AddMonitorLogAsync(chainId, record.BlockHeight,
                            ContractAppServiceConstant.MainChainId,
                            result.BlockNumber,
                            record.ChangeType);
                        await UpdateSyncHolderVersionAsync(ContractAppServiceConstant.MainChainId, record.CaHash, record.ValidateHeight);
                        _logger.LogInformation("{type} SyncToMain succeed on chain: {id} of account: {hash}",
                            record.ChangeType, chainId, record.CaHash);
                    }

                    record = records.FirstOrDefault(r => r.ValidateHeight < indexHeight);
                }
            }

            await _recordsBucketContainer.AddToBeValidatedRecordsAsync(chainId, failedRecords);
            await _recordsBucketContainer.SetValidatedRecordsAsync(chainId, records);

            _logger.LogInformation(
                "SyncQueryEvents on chain: {id} Ends, synced {num} events and failed {failedNum} events", chainId,
                recordsAmount - records.Count, failedRecords.Count);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SyncQueryEventsAsync on chain: {id} Error", chainId);
        }
    }

    private async Task QueryEventsAsync(string chainId)
    {
        _logger.LogInformation("QueryEvents on chain: {id} starts", chainId);

        try
        {
            var lastEndHeight = await _graphQLProvider.GetLastEndHeightAsync(chainId, QueryType.QueryRecord);

            var currentIndexHeight = await _graphQLProvider.GetIndexBlockHeightAsync(chainId);

            var targetIndexHeight = currentIndexHeight + _indexOptions.IndexAfter;

            if (currentIndexHeight <= 0 || lastEndHeight >= targetIndexHeight)
            {
                _logger.LogWarning(
                    "QueryEventsAsync on chain: {id}. Index Height is not enough. Skipped querying this time. \nLastEndHeight: {last}, CurrentIndexHeight: {index}",
                    chainId, lastEndHeight, currentIndexHeight);
                return;
            }

            var startIndexHeight = lastEndHeight - _indexOptions.IndexBefore;
            var endIndexHeight = lastEndHeight + _indexOptions.IndexInterval;
            endIndexHeight = endIndexHeight < targetIndexHeight ? endIndexHeight : targetIndexHeight;

            List<QueryEventDto> queryEvents = new List<QueryEventDto>();

            while (endIndexHeight <= targetIndexHeight)
            {
                _logger.LogInformation("Query on chain: {id}, from {start} to {end}", chainId, startIndexHeight,
                    endIndexHeight);

                queryEvents.AddRange(await _graphQLProvider.GetLoginGuardianTransactionInfosAsync(
                    chainId, startIndexHeight, endIndexHeight));
                queryEvents.AddRange(await _graphQLProvider.GetManagerTransactionInfosAsync(
                    chainId, startIndexHeight, endIndexHeight));
                queryEvents.AddRange(await _graphQLProvider.GetGuardianTransactionInfosAsync(
                    chainId, startIndexHeight, endIndexHeight));

                if (endIndexHeight == targetIndexHeight)
                {
                    break;
                }

                startIndexHeight = endIndexHeight;

                endIndexHeight += _indexOptions.IndexInterval;
                endIndexHeight = endIndexHeight < targetIndexHeight ? endIndexHeight : targetIndexHeight;
            }

            if (queryEvents.IsNullOrEmpty())
            {
                _logger.LogInformation(
                    "Found no events on chain: {id}. Next index block height: {height}", chainId,
                    currentIndexHeight);
            }
            else
            {
                _logger.LogInformation(
                    "Found {num} events on chain: {id}. Next index block height: {height}", queryEvents.Count, chainId,
                    currentIndexHeight);

                queryEvents = queryEvents.Where(e => e.ChangeType != QueryLoginGuardianType.LoginGuardianRemoved)
                    .ToList();

                var list = OptimizeQueryEvents(queryEvents);

                list = RemoveDuplicateQueryEvents(await _recordsBucketContainer.GetValidatedRecordsAsync(chainId),
                    list);

                await _monitorLogProvider.InitDataSyncMonitorAsync(list, chainId);

                await _recordsBucketContainer.AddToBeValidatedRecordsAsync(chainId, list);
            }

            await _graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.QueryRecord, currentIndexHeight);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "QueryEventsAsync on chain: {id} Error", chainId);
        }
    }

    private async Task ValidateQueryEventsAsync(string chainId)
    {
        _logger.LogInformation("ValidateQueryEvents on chain: {id} starts", chainId);

        try
        {
            var validatedRecords = new List<SyncRecord>();
            var failedRecords = new List<SyncRecord>();

            var storedToBeValidatedRecords = await _recordsBucketContainer.GetToBeValidatedRecordsAsync(chainId);

            if (storedToBeValidatedRecords.IsNullOrEmpty())
            {
                _logger.LogInformation("Found no events on chain: {id} to validate", chainId);
                return;
            }

            storedToBeValidatedRecords = OptimizeSyncRecords(storedToBeValidatedRecords
                .Where(r => r.RetryTimes <= _indexOptions.MaxRetryTimes).ToList());

            foreach (var record in storedToBeValidatedRecords)
            {
                _logger.LogInformation(
                    "Event type: {type} validate starting on chain: {id} of account: {hash} at Height: {height}",
                    record.ChangeType, chainId, record.CaHash, record.BlockHeight);

                var unsetLoginGuardians = new RepeatedField<string>();
                if (record.NotLoginGuardian != null)
                {
                    unsetLoginGuardians.Add(record.NotLoginGuardian);
                }

                var currentBlockHeight = await _contractProvider.GetBlockHeightAsync(chainId);

                if (currentBlockHeight <= record.BlockHeight)
                {
                    _logger.LogWarning(LoggerMsg.NodeBlockHeightWarning);
                    break;
                }

                var outputGetHolderInfo =
                    await _contractProvider.GetHolderInfoFromChainAsync(chainId, Hash.Empty, record.CaHash);
                var transactionDto =
                    await _contractProvider.ValidateTransactionAsync(chainId, outputGetHolderInfo,
                        unsetLoginGuardians);

                if (transactionDto.TransactionResultDto.Status != TransactionState.Mined)
                {
                    _logger.LogError("ValidateQueryEvents on chain: {id} of account: {hash} failed",
                        chainId, record.CaHash);
                    record.RetryTimes++;

                    failedRecords.Add(record);
                }
                else
                {
                    record.ValidateHeight = transactionDto.TransactionResultDto.BlockNumber;
                    record.ValidateTransactionInfoDto = new TransactionInfo
                    {
                        BlockNumber = transactionDto.TransactionResultDto.BlockNumber,
                        TransactionId = transactionDto.TransactionResultDto.TransactionId,
                        Transaction = transactionDto.Transaction.ToByteArray()
                    };
                    validatedRecords.Add(record);

                    _monitorLogProvider.AddNode(record, DataSyncType.EndValidate);
                }
            }

            await _recordsBucketContainer.AddValidatedRecordsAsync(chainId, validatedRecords);
            await _recordsBucketContainer.SetToBeValidatedRecordsAsync(chainId, failedRecords);

            _logger.LogInformation(
                "ValidateQueryEvents on chain: {id} ends, validated {num} events and failed {failedNum} events",
                chainId, validatedRecords.Count, failedRecords.Count);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ValidateQueryEvents on chain {id} error", chainId);
        }
    }

    private List<SyncRecord> OptimizeQueryEvents(List<QueryEventDto> queryEvents)
    {
        var list = queryEvents.Select(dto => new SyncRecord
            {
                BlockHeight = dto.BlockHeight,
                BlockHash = dto.BlockHash,
                Manager = dto.Manager,
                CaHash = dto.CaHash,
                ChangeType = dto.ChangeType,
                NotLoginGuardian = dto.NotLoginGuardian,
                ValidateHeight = long.MaxValue,
            })
            .ToList();

        list = OptimizeSyncRecords(list);

        return list;
    }

    private List<SyncRecord> OptimizeSyncRecords(List<SyncRecord> records)
    {
        var index = records.Count - 1;
        while (index > 0)
        {
            var neededDeleteEvent = records.FirstOrDefault(e =>
                e.ChangeType != QueryLoginGuardianType.LoginGuardianUnbound &&
                e.BlockHeight < records[index].BlockHeight && e.CaHash == records[index].CaHash);
            if (neededDeleteEvent != null)
            {
                records.Remove(neededDeleteEvent);
            }

            index--;
        }

        return records;
    }

    private List<SyncRecord> RemoveDuplicateQueryEvents(List<SyncRecord> previousList, List<SyncRecord> newList)
    {
        if (newList.IsNullOrEmpty())
        {
            return new List<SyncRecord>();
        }

        if (previousList.IsNullOrEmpty())
        {
            return newList;
        }

        var list = new List<SyncRecord>();

        foreach (var record in newList)
        {
            if (previousList.Any(r =>
                    r.BlockHash == record.BlockHash && r.Manager == record.Manager &&
                    r.ChangeType == record.ChangeType))
            {
                continue;
            }

            list.Add(record);
        }

        return list;
    }

    public async Task InitializeIndexAsync()
    {
        var dict = _indexOptions.AutoSyncStartHeight;

        foreach (var info in _chainOptions.ChainInfos)
        {
            var chainId = info.Key;
            var result = dict.TryGetValue(chainId, out var height);
            if (!result)
            {
                height = 0;
            }

            var queryRecordHeight = await _graphQLProvider.GetLastEndHeightAsync(chainId, QueryType.QueryRecord);

            if (queryRecordHeight < height)

            {
                _logger.LogInformation("InitializeIndexAsync on chain {id} set last end height to {height}", chainId,
                    height);
                await _graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.QueryRecord, height);
            }
        }
    }

    private async Task AddMonitorLogAsync(string startChainId, long startHeight, string endChainId, long endHeight,
        string changeType)
    {
        try
        {
            if (!_indicatorLogger.IsEnabled()) return;

            var startBlock = await _contractProvider.GetBlockByHeightAsync(startChainId, startHeight);
            var endBlock = await _contractProvider.GetBlockByHeightAsync(endChainId, endHeight);
            var blockInterval = endBlock.Header.Time - startBlock.Header.Time;
            var duration = (int)blockInterval.TotalMilliseconds;
            _indicatorLogger.LogInformation(MonitorTag.ChainDataSync, changeType, duration);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "add monitor log error.");
        }
    }

    private async Task AddHeightIndexMonitorLogAsync(string chainId, long indexHeight)
    {
        try
        {
            if (!_indicatorLogger.IsEnabled()) return;

            var height = await _contractProvider.GetBlockHeightAsync(chainId);
            var duration = (int)Math.Abs(height - indexHeight);
            _indicatorLogger.LogInformation(MonitorTag.DataSyncHeightIndex, chainId,
                duration);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "add height index monitor log error.");
        }
    }
    
    private async Task<bool> CheckSyncHolderVersionAsync(string targetChainId, string caHash, long updateVersion)
    {
        var cacheKey = $"{ContractEventConstants.SyncHolderUpdateVersionCachePrefix}:{targetChainId}:{caHash}";
        var lastUpdateVersion = await _distributedCache.GetAsync(cacheKey);
        if (!lastUpdateVersion.IsNullOrWhiteSpace() && long.Parse(lastUpdateVersion) > updateVersion)
        {
            _logger.LogInformation("skip syncHolder targetChainId: {chainId}, caHash :{caHash},lastUpdateVersion:{version},curVersion:{curVersion}", 
                targetChainId, caHash, lastUpdateVersion, updateVersion);
            return false;
        }

        return true;
    }
    
    private async Task UpdateSyncHolderVersionAsync(string targetChainId, string caHash, long updateVersion)
    {
        var cacheKey = $"{ContractEventConstants.SyncHolderUpdateVersionCachePrefix}:{targetChainId}:{caHash}";
        var lastUpdateVersion = await _distributedCache.GetAsync(cacheKey);
        if (lastUpdateVersion.IsNullOrWhiteSpace() || long.Parse(lastUpdateVersion) < updateVersion)
        {
            await _distributedCache.SetAsync(cacheKey, updateVersion.ToString(), new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromSeconds(ContractEventConstants.SyncHolderUpdateVersionCacheExpireTime)
            });
        }
    }
}
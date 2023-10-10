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
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Portkey.Contracts.CA;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IContractAppService
{
    Task CreateHolderInfoAsync(AccountRegisterCreateEto message);
    Task SocialRecoveryAsync(AccountRecoverCreateEto message);
    Task QueryAndSyncAsync();
    Task InitializeIndexAsync();
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

    public ContractAppService(IDistributedEventBus distributedEventBus, IOptionsSnapshot<ChainOptions> chainOptions,
        IOptionsSnapshot<IndexOptions> indexOptions, IGraphQLProvider graphQLProvider,
        IContractProvider contractProvider, IObjectMapper objectMapper, ILogger<ContractAppService> logger,
        IRecordsBucketContainer recordsBucketContainer, IIndicatorLogger indicatorLogger)
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
        _ = ValidateTransactionAndAddSyncRecordAsync(createHolderDto.ChainId, outputGetHolderInfo, "", MonitorTag.Register);
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

        var holderInfo = await _contractProvider.GetHolderInfoFromChainAsync(socialRecoveryDto.ChainId,
            socialRecoveryDto.LoginGuardianIdentifierHash, null);

        if (holderInfo.CaHash == null || holderInfo.CaHash.Value.IsEmpty)
        {
            recoveryResult.RecoveryMessage = "No account found";
            recoveryResult.RecoverySuccess = false;

            _logger.LogInformation("Recovery state pushed: " + "\n{result}",
                JsonConvert.SerializeObject(recoveryResult, Formatting.Indented));

            return;
        }

        recoveryResult.RecoverySuccess = true;
        recoveryResult.CaHash = holderInfo.CaHash.ToHex();
        recoveryResult.CaAddress = holderInfo.CaAddress.ToBase58();

        await _distributedEventBus.PublishAsync(recoveryResult);

        _logger.LogInformation("Recovery state pushed: " + "\n{result}",
            JsonConvert.SerializeObject(recoveryResult, Formatting.Indented));

        // ValidateAndSync can be very time consuming, so don't wait for it to finish
        _ = ValidateTransactionAndAddSyncRecordAsync(socialRecoveryDto.ChainId, holderInfo, "",
            MonitorTag.SocialRecover);
    }

    private async Task ValidateTransactionAndAddSyncRecordAsync(string chainId, GetHolderInfoOutput holderInfo,
        string optionChainId, MonitorTag target)
    {
        var stopwatch = Stopwatch.StartNew();
        await ValidateTransactionAndAddSyncRecordAsync(chainId, holderInfo, optionChainId);
        stopwatch.Stop();

        var duration = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
        _indicatorLogger.LogInformation(MonitorTag.ChainDataSync, target.ToString(), duration);
    }

    private async Task ValidateTransactionAndAddSyncRecordAsync(string chainId, GetHolderInfoOutput holderInfo,
        string optionChainId)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        var transactionDto =
            await _contractProvider.ValidateTransactionAsync(chainId, holderInfo, null);

        var syncRecord = new SyncRecord
        {
            ValidateHeight = transactionDto.TransactionResultDto.BlockNumber,
            ValidateTransactionInfoDto = new TransactionInfo
            {
                BlockNumber = transactionDto.TransactionResultDto.BlockNumber,
                TransactionId = transactionDto.TransactionResultDto.TransactionId,
                Transaction = transactionDto.Transaction.ToByteArray()
            }
        };
        await _recordsBucketContainer.AddValidatedRecordsAsync(chainId, new List<SyncRecord> { syncRecord });
        // var validateHeight = transactionDto.TransactionResultDto.BlockNumber;
        // var tasks = new List<Task<bool>>();
        // if (chainInfo.IsMainChain)
        // {
        //     tasks.AddRange(_chainOptions.ChainInfos.Values.Where(c => !c.IsMainChain && c.ChainId != optionChainId)
        //         .Select(sideChain => new MainChainHolderInfoSyncWorker(_contractProvider, _logger, _indicatorLogger)
        //             .SyncAsync(sideChain.ChainId, chainId, validateHeight, transactionDto)));
        // }
        // else
        // {
        //     tasks.Add(new SideChainHolderInfoSyncWorker(_contractProvider, _logger, _indicatorLogger)
        //         .SyncAsync(chainId, ContractAppServiceConstant.MainChainId, validateHeight, transactionDto));
        // }
        //
        // await tasks.WhenAll();
    }

    private async Task<bool> CheckHolderExistsOnBothChains(Hash loginGuardian)
    {
        var tasks = _chainOptions.ChainInfos.Keys.Select(chainId => CheckHolderExists(chainId, loginGuardian)).ToList();
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
        var tasks = _chainOptions.ChainInfos.Keys.Select(QueryEventsAndSyncAsync).ToList();
        await tasks.WhenAll();
    }

    private async Task QueryEventsAndSyncAsync(string chainId)
    {
        await QueryEventsAsync(chainId);
        var tasks = new List<Task>(2)
        {
            ValidateQueryEventsAsync(chainId),
            SyncQueryEventsAsync(chainId)
        };
        await tasks.WhenAll();
    }

    private async Task SyncQueryEventsAsync(string chainId)
    {
        _logger.LogInformation("SyncQueryEvents on chain: {id} starts", chainId);

        try
        {
            var records = await _recordsBucketContainer.GetValidatedRecordsAsync(chainId);
            if (records.IsNullOrEmpty())
            {
                _logger.LogInformation("Found no record to sync on chain: {id}", chainId);
                return;
            }

            foreach (var record in records)
            {
                record.RecordStatus = RecordStatus.NONE;
            }

            var recordsAmount = records.Count;

            _logger.LogInformation("Found {count} records to sync on chain: {id}", records.Count, chainId);

            var tasks = new List<Task>();
            List<SyncRecord> targetRecords;
            if (chainId == ContractAppServiceConstant.MainChainId)
            {
                foreach (var info in _chainOptions.ChainInfos.Values.Where(info => !info.IsMainChain))
                {
                    var indexHeight = await _contractProvider.GetIndexHeightFromSideChainAsync(info.ChainId);

                    targetRecords = records.Where(r => r.ValidateHeight < indexHeight).ToList();
                    tasks.AddRange(targetRecords.Select(record => new MainChainHolderInfoSyncWorker(_contractProvider, _logger, _indicatorLogger)
                        .ProcessSyncRecord(chainId, info.ChainId, record, _indexOptions)));
                }
            }
            else
            {
                var indexHeight = await _contractProvider.GetIndexHeightFromMainChainAsync(
                    ContractAppServiceConstant.MainChainId, await _contractProvider.GetChainIdAsync(chainId));

                targetRecords = records.Where(r => r.ValidateHeight < indexHeight).ToList();
                tasks.AddRange(targetRecords.Select(record => new SideChainHolderInfoSyncWorker(_contractProvider, _logger, _indicatorLogger)
                    .ProcessSyncRecord(ContractAppServiceConstant.MainChainId, chainId, record, _indexOptions)));
            }

            await tasks.WhenAll();
            var failedRecords = records.Where(r => r.RecordStatus == RecordStatus.NOT_MINED).ToList();
            await _recordsBucketContainer.AddToBeValidatedRecordsAsync(chainId, failedRecords);
            var leftRecords = records.Where(r => r.RecordStatus == RecordStatus.NONE).ToList();
            await _recordsBucketContainer.SetValidatedRecordsAsync(chainId, leftRecords);

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

            var queryEvents = new List<QueryEventDto>();

            while (endIndexHeight <= targetIndexHeight)
            {
                _logger.LogInformation("Query on chain: {id}, from {start} to {end}", chainId, startIndexHeight,
                    endIndexHeight);
                var tasks = new List<Task<List<QueryEventDto>>>()
                {
                    _graphQLProvider.GetLoginGuardianTransactionInfosAsync(
                        chainId, startIndexHeight, endIndexHeight),
                    _graphQLProvider.GetManagerTransactionInfosAsync(
                        chainId, startIndexHeight, endIndexHeight)
                };
                var ansList = await tasks.WhenAll();
                foreach (var ans in ansList)
                {
                    queryEvents.AddRange(ans);
                }

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
            var currentBlockHeight = await _contractProvider.GetBlockHeightAsync(chainId);
            var targetRecords = storedToBeValidatedRecords.Where(record => record.BlockHeight < currentBlockHeight).ToList();
            var tasks = storedToBeValidatedRecords.Select(record => ProcessValidatedRecord(chainId, record)).ToList();
            await tasks.WhenAll();
            await _recordsBucketContainer.AddValidatedRecordsAsync(chainId, targetRecords.Where(r => r.RecordStatus == RecordStatus.MINED).ToList());
            var leftRecords = targetRecords.Where(r => r.RecordStatus == RecordStatus.NOT_MINED).ToList();
            leftRecords.AddRange(storedToBeValidatedRecords.Where(record => record.BlockHeight >= currentBlockHeight));
            await _recordsBucketContainer.SetToBeValidatedRecordsAsync(chainId, leftRecords);

            _logger.LogInformation(
                "ValidateQueryEvents on chain: {id} ends, validated {num} events and failed {failedNum} events",
                chainId, validatedRecords.Count, failedRecords.Count);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ValidateQueryEvents on chain {id} error", chainId);
        }
    }

    private async Task ProcessValidatedRecord(string chainId, List<SyncRecord> records)
    {
        var holderInfoList = new List<GetHolderInfoOutput>();
        var unsetLoginGuardiansList = new List<RepeatedField<string>>();
        foreach (var record in records)
        {
            record.RecordStatus = RecordStatus.NONE;
            _logger.LogInformation(
                "Event type: {type} validate starting on chain: {id} of account: {hash} at Height: {height}",
                record.ChangeType, chainId, record.CaHash, record.BlockHeight);


            var unsetLoginGuardians = new RepeatedField<string>();
            if (record.NotLoginGuardian != null)
            {
                unsetLoginGuardians.Add(record.NotLoginGuardian);
            }
            unsetLoginGuardiansList.Add(unsetLoginGuardians);

            // single view from node
            var holderInfo = await _contractProvider.GetHolderInfoFromChainAsync(chainId, Hash.Empty, record.CaHash);
            holderInfoList.Add(holderInfo);
        }

        var transactionDtoList =
            await _contractProvider.ValidateTransactionListAsync(chainId, holderInfoList, unsetLoginGuardiansList);

        for (int i = 0; i < transactionDtoList.Count; i++)
        {
            var transactionDto = transactionDtoList[i];
            var record = records[i];
            if (record == null)
            {
                continue;
            }
            if (transactionDto.TransactionResultDto.Status != TransactionState.Mined)
            {
                _logger.LogError("ValidateQueryEvents on chain: {id} of account: {hash} failed",
                    chainId, record.CaHash);
                record.RetryTimes++;
                record.RecordStatus = RecordStatus.NOT_MINED;
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
                record.RecordStatus = RecordStatus.MINED;
            }
        }
    }

    private async Task ProcessValidatedRecord(string chainId, SyncRecord record)
    {
        record.RecordStatus = RecordStatus.NONE;
        _logger.LogInformation(
            "Event type: {type} validate starting on chain: {id} of account: {hash} at Height: {height}",
            record.ChangeType, chainId, record.CaHash, record.BlockHeight);


        var unsetLoginGuardians = new RepeatedField<string>();
        if (record.NotLoginGuardian != null)
        {
            unsetLoginGuardians.Add(record.NotLoginGuardian);
        }

        var holderInfo = await _contractProvider.GetHolderInfoFromChainAsync(chainId, Hash.Empty, record.CaHash);
        var transactionDto = await _contractProvider.ValidateTransactionAsync(chainId, holderInfo, unsetLoginGuardians);

        if (transactionDto.TransactionResultDto.Status != TransactionState.Mined)
        {
            _logger.LogError("ValidateQueryEvents on chain: {id} of account: {hash} failed",
                chainId, record.CaHash);
            record.RetryTimes++;
            record.RecordStatus = RecordStatus.NOT_MINED;
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
            record.RecordStatus = RecordStatus.MINED;
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
                ValidateHeight = long.MaxValue
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

        return previousList.IsNullOrEmpty() ? newList : newList.Where(record => !previousList.Any(r => r.BlockHash == record.BlockHash && r.Manager == record.Manager && r.ChangeType == record.ChangeType)).ToList();
    }

    public async Task InitializeIndexAsync()
    {
        var dict = _indexOptions.AutoSyncStartHeight;

        foreach (var chainId in _chainOptions.ChainInfos.Select(info => info.Key))
        {
            var result = dict.TryGetValue(chainId, out var height);
            if (!result)
            {
                height = 0;
            }

            var queryRecordHeight = await _graphQLProvider.GetLastEndHeightAsync(chainId, QueryType.QueryRecord);

            if (queryRecordHeight >= height)
            {
                continue;
            }

            _logger.LogInformation("InitializeIndexAsync on chain {id} set last end height to {height}", chainId,
                height);
            await _graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.QueryRecord, height);
        }
    }
}
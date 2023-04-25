using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.Etos;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.State.ApplicationHandler;
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
    Task InitializeIndexAsync(long blockHeight);
}

public class ContractAppService : IContractAppService
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ChainOptions _chainOptions;
    private readonly IndexOptions _indexOptions;
    private readonly IGraphQLProvider _graphQLProvider;
    private readonly IContractProvider _contractProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ContractAppService> _logger;

    public ContractAppService(IDistributedEventBus distributedEventBus, IOptions<ChainOptions> chainOptions,
        IOptions<IndexOptions> indexOptions, IGraphQLProvider graphQLProvider, IContractProvider contractProvider,
        IObjectMapper objectMapper, ILogger<ContractAppService> logger)
    {
        _distributedEventBus = distributedEventBus;
        _indexOptions = indexOptions.Value;
        _chainOptions = chainOptions.Value;
        _graphQLProvider = graphQLProvider;
        _contractProvider = contractProvider;
        _objectMapper = objectMapper;
        _logger = logger;
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
        _ = ValidateTransactionAndSyncAsync(createHolderDto.ChainId, outputGetHolderInfo, "");
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

        // ValidateAndSync can be very time consuming, so don't wait for it to finish
        _ = ValidateTransactionAndSyncAsync(socialRecoveryDto.ChainId, outputGetHolderInfo, "");
    }

    private async Task<bool> ValidateTransactionAndSyncAsync(string chainId, GetHolderInfoOutput result,
        string optionChainId)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        var transactionDto =
            await _contractProvider.ValidateTransactionAsync(chainId, result, null);
        var syncHolderInfoInput =
            await _contractProvider.GetSyncHolderInfoInputAsync(chainId, transactionDto);

        var syncSucceed = true;

        if (syncHolderInfoInput.VerificationTransactionInfo == null)
        {
            return false;
        }

        if (chainInfo.IsMainChain)
        {
            foreach (var sideChain in _chainOptions.ChainInfos.Values.Where(c =>
                         !c.IsMainChain && c.ChainId != optionChainId))
            {
                await _contractProvider.SideChainCheckMainChainBlockIndexAsync(sideChain.ChainId,
                    syncHolderInfoInput.VerificationTransactionInfo.ParentChainHeight);

                var resultDto = await _contractProvider.SyncTransactionAsync(sideChain.ChainId, syncHolderInfoInput);
                syncSucceed = syncSucceed && resultDto.Status == TransactionState.Mined;
            }
        }
        else
        {
            var syncResult =
                await _contractProvider.SyncTransactionAsync(ContractAppServiceConstant.MainChainId,
                    syncHolderInfoInput);

            // result = await _contractProvider.GetHolderInfoFromChainAsync(ContractAppServiceConstant.MainChainId, Hash.Empty, result.CaHash.ToHex());
            //
            // syncSucceed =
            //     await ValidateTransactionAndSyncAsync(ContractAppServiceConstant.MainChainId, result, chainId);
            syncSucceed = syncResult.Status == TransactionState.Mined;
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
            _logger.LogInformation("QueryEventsAndSync on chain: {id} Starts", chainId);
            tasks.Add(QueryEventsAndSyncAsync(chainId));
        }

        await tasks.WhenAll();
    }

    private async Task QueryEventsAndSyncAsync(string chainId)
    {
        await QueryEventsAsync(chainId);
        await SyncQueryEventsAsync(chainId);
    }

    private long GetEndBlockHeight(long lastEndHeight, long interval, long currentIndexHeight)
    {
        if (lastEndHeight + 1 >= currentIndexHeight - _indexOptions.IndexSafe)
        {
            return ContractAppServiceConstant.LongError;
        }

        var nextQueryHeight = lastEndHeight + 1 + interval;
        var currentSafeIndexHeight = currentIndexHeight - _indexOptions.IndexSafe;

        return nextQueryHeight < currentSafeIndexHeight ? nextQueryHeight : currentSafeIndexHeight;
    }

    private async Task SyncQueryEventsAsync(string chainId)
    {
        var failedRecords = new List<SyncRecord>();
        
        var records = await _contractProvider.GetSyncRecords(chainId);
        
        if (records.IsNullOrEmpty())
        {
            _logger.LogInformation("Found no record to sync on chain: {id}", chainId);
            return;
        }
        
        if (chainId == ContractAppServiceConstant.MainChainId)
        {
            foreach (var info in _chainOptions.ChainInfos.Values.Where(info => !info.IsMainChain))
            {
                var indexHeight = await _contractProvider.GetIndexHeightFromSideChainAsync(info.ChainId);

                var record = records.FirstOrDefault(r => r.ValidateHeight < indexHeight);

                while (record != null)
                {
                    var syncHolderInfoInput =
                        await _contractProvider.GetSyncHolderInfoInputAsync(chainId, record.ValidateTransactionInfoDto);
                    var result = await _contractProvider.SyncTransactionAsync(info.ChainId, syncHolderInfoInput);
                    
                    if (result.Status != TransactionState.Mined)
                    {
                        _logger.LogError("{type} SyncToSide failed on chain: {id} of account: {hash}, error: {error}",
                            record.ChangeType, chainId, record.CaHash, result.Error);

                        record.RetryTimes++;
                        failedRecords.Add(record);
                    }
                    
                    _logger.LogInformation("{type} SyncToSide succeed on chain: {id} of account: {hash}",
                        record.ChangeType, chainId, record.CaHash);
                    records.Remove(record);
                    
                    record = records.FirstOrDefault(r => r.ValidateHeight < indexHeight);
                }
            }
        }
        else
        {
            var indexHeight = await _contractProvider.GetIndexHeightFromMainChainAsync(ContractAppServiceConstant.MainChainId, await _contractProvider.GetChainIdAsync(chainId));

            var record = records.FirstOrDefault(r => r.ValidateHeight < indexHeight);
            
            while (record != null)
            {

                var mainHeight = await _contractProvider.GetBlockHeightAsync(ContractAppServiceConstant.MainChainId);

                var checkResult = false;
                while (!checkResult)
                {
                    var indexMainChainBlock = await _contractProvider.GetIndexHeightFromSideChainAsync(chainId);
                    checkResult = indexMainChainBlock > mainHeight;
                }
                
                var syncHolderInfoInput =
                    await _contractProvider.GetSyncHolderInfoInputAsync(chainId, record.ValidateTransactionInfoDto);
                var result = await _contractProvider.SyncTransactionAsync(ContractAppServiceConstant.MainChainId, syncHolderInfoInput);
                    
                if (result.Status != TransactionState.Mined)
                {
                    _logger.LogError("{type} SyncToMain failed on chain: {id} of account: {hash}, error: {error}",
                        record.ChangeType, chainId, record.CaHash, result.Error);

                    record.RetryTimes++;
                    failedRecords.Add(record);
                }
                    
                _logger.LogInformation("{type} SyncToMain succeed on chain: {id} of account: {hash}",
                    record.ChangeType, chainId, record.CaHash);
                records.Remove(record);
                    
                record = records.FirstOrDefault(r => r.ValidateHeight < indexHeight);
            }
        }

        await _contractProvider.AddFailedRecordsAsync(chainId, failedRecords);
    }

    private async Task QueryEventsAsync(string chainId)
    {
        try
        {
            var lastEndHeight = await _graphQLProvider.GetLastEndHeightAsync(chainId, QueryType.QueryRecord);
            var currentIndexHeight = await _graphQLProvider.GetIndexBlockHeightAsync(chainId);
            var endBlockHeight = GetEndBlockHeight(lastEndHeight, _indexOptions.IndexInterval, currentIndexHeight);

            if (endBlockHeight == ContractAppServiceConstant.LongError)
            {
                _logger.LogWarning(
                    "QueryEventsAsync on chain: {id}. Index Height is not enough. Skipped querying this time. \nLastEndHeight: {last}, CurrentIndexHeight: {index}",
                    chainId, lastEndHeight, currentIndexHeight);
                return;
            }

            var queryEvents = await _graphQLProvider.GetLoginGuardianTransactionInfosAsync(
                chainId, lastEndHeight + 1, endBlockHeight);
            queryEvents.AddRange(await _graphQLProvider.GetManagerTransactionInfosAsync(
                chainId, lastEndHeight + 1, endBlockHeight));

            var nextIndexHeight = endBlockHeight < currentIndexHeight
                ? endBlockHeight + 1
                : currentIndexHeight - _indexOptions.IndexSafe;

            await _graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.QueryRecord, nextIndexHeight);

            if (queryEvents.IsNullOrEmpty())
            {
                _logger.LogInformation(
                    "Found no events on chain: {id}. Next index block height: {height}", chainId,
                    nextIndexHeight);
                return;
            }

            queryEvents = OptimizeQueryEvents(queryEvents);
            _logger.LogInformation(
                "Found {num} events on chain: {id}", queryEvents.Count, chainId);

            var list = new List<SyncRecord>();
            foreach (var dto in queryEvents)
            {
                list.Add(new SyncRecord
                {
                    BlockHeight = dto.BlockHeight,
                    CaHash = dto.CaHash,
                    ChangeType = dto.ChangeType,
                    NotLoginGuardian = dto.NotLoginGuardian
                });
            }

            await ValidateQueryEventsAsync(chainId, list);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "QueryEventsAsync on chain: {id} Error", chainId);
        }
    }

    private async Task ValidateQueryEventsAsync(string chainId, List<SyncRecord> records)
    {
        try
        {
            var validatedRecords = new List<SyncRecord>();
            var failedRecords = new List<SyncRecord>();

            List<SyncRecord> recordsToValidate = new List<SyncRecord>();

            var storedFaileRecords = await _contractProvider.GetFailedRecords(chainId);
            if (!storedFaileRecords.IsNullOrEmpty())
            {
                recordsToValidate.AddRange(storedFaileRecords);
            }
            
            await _contractProvider.ClearFailedRecordsAsync(chainId);
            
            recordsToValidate.AddRange(records);
            
            foreach (var record in recordsToValidate)
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
                    record.RetryTimes++;
                    failedRecords.Add(record);
                }

                record.ValidateHeight = transactionDto.TransactionResultDto.BlockNumber;
                record.ValidateTransactionInfoDto = transactionDto;
                validatedRecords.Add(record);
            }

            await _contractProvider.AddSyncRecordsAsync(chainId, validatedRecords);
            await _contractProvider.AddFailedRecordsAsync(chainId, failedRecords);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ValidateQueryEvents error");
        }
    }

    private List<QueryEventDto> OptimizeQueryEvents(List<QueryEventDto> queryEvents)
    {
        var index = queryEvents.Count - 1;
        while (index > 0)
        {
            var neededDeleteEvent = queryEvents.FirstOrDefault(e =>
                e.ChangeType != QueryLoginGuardianType.LoginGuardianUnbound &&
                e.BlockHeight < queryEvents[index].BlockHeight && e.CaHash == queryEvents[index].CaHash);
            if (neededDeleteEvent != null)
            {
                queryEvents.Remove(neededDeleteEvent);
            }

            index--;
        }

        return queryEvents;
    }

    public async Task InitializeIndexAsync()
    {
        var tasks = new List<Task>();
        foreach (var chainId in _chainOptions.ChainInfos.Keys)
        {
            var loginGuardianHeight = await _graphQLProvider.GetLastEndHeightAsync(chainId, QueryType.LoginGuardian);
            var managerInfoHeight = await _graphQLProvider.GetLastEndHeightAsync(chainId, QueryType.ManagerInfo);

            var indexHeight = await _graphQLProvider.GetIndexBlockHeightAsync(chainId);
            if (loginGuardianHeight == 0)
            {
                tasks.Add(_graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.LoginGuardian,
                    indexHeight - _indexOptions.IndexSafe));
            }

            if (managerInfoHeight == 0)
            {
                tasks.Add(_graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.ManagerInfo,
                    indexHeight - _indexOptions.IndexSafe));
            }

            await _graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.QueryRecord, loginGuardianHeight);
        }

        await tasks.WhenAll();
    }

    public async Task InitializeIndexAsync(long blockHeight)
    {
        var tasks = new List<Task>();
        foreach (var chainId in _chainOptions.ChainInfos.Keys)
        {
            tasks.Add(_graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.LoginGuardian, blockHeight));
            tasks.Add(_graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.ManagerInfo, blockHeight));
        }

        await tasks.WhenAll();
    }
}
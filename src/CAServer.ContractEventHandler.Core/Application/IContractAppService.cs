using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.Etos;
using CAServer.Grains.Grain.ApplicationHandler;
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
    Task QueryEventsAndSyncAsync();
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
        
        ValidateTransactionAndSyncAsync(createHolderDto.ChainId, outputGetHolderInfo, "");
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

        ValidateTransactionAndSyncAsync(socialRecoveryDto.ChainId, outputGetHolderInfo, "");
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

    public async Task QueryEventsAndSyncAsync()
    {
        foreach (var chainId in _chainOptions.ChainInfos.Keys)
        {
            _logger.LogInformation("QueryEventsAndSync on chain: {id} Starts", chainId);
            await QueryLoginGuardianEventsAndSyncAsync(chainId);
            await QueryManagerEventsAndSyncAsync(chainId);
        }
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

    private async Task SyncQueryEventsAsync(string chainId, long lastChainHeight,
        Dictionary<QueryEventDto, SyncHolderInfoInput> dict, string queryType)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        if (chainInfo.IsMainChain)
        {
            foreach (var info in _chainOptions.ChainInfos.Values.Where(info => !info.IsMainChain))
            {
                await _contractProvider.SideChainCheckMainChainBlockIndexAsync(info.ChainId, lastChainHeight);

                foreach (var dto in dict.Keys)
                {
                    var result = await _contractProvider.SyncTransactionAsync(info.ChainId, dict[dto]);
                    if (result.Status != TransactionState.Mined)
                    {
                        _logger.LogError("{type} SyncToSide failed on chain: {id} of account: {hash}, error: {error}",
                            dto.ChangeType, chainInfo.ChainId, dto.CaHash, result.Error);
                        return;
                    }

                    await _graphQLProvider.SetLastEndHeightAsync(chainId, queryType, dto.BlockHeight);
                    _logger.LogInformation("{type} SyncToSide succeed on chain: {id} of account: {hash}",
                        dto.ChangeType, chainInfo.ChainId, dto.CaHash);
                }
            }
        }
        else
        {
            foreach (var dto in dict.Keys)
            {
                var result =
                    await _contractProvider.SyncTransactionAsync(ContractAppServiceConstant.MainChainId, dict[dto]);
                if (result.Status != TransactionState.Mined)
                {
                    _logger.LogError("{type} SyncToMain failed on chain: {id} of account: {hash}, error: {error}",
                        dto.ChangeType, ContractAppServiceConstant.MainChainId, dto.CaHash, result.Error);
                    return;
                }

                // var output =
                //     await _contractProvider.GetHolderInfoFromChainAsync(ContractAppServiceConstant.MainChainId,
                //         Hash.Empty, dto.CaHash);
                //
                // var syncResult =
                //     await ValidateTransactionAndSyncAsync(ContractAppServiceConstant.MainChainId, output, chainId);
                //
                // if (!syncResult)
                // {
                //     _logger.LogError("{type} SyncToOthers failed on chain: {id} of account: {hash}, error: {error}",
                //         dto.ChangeType, chainInfo.ChainId, dto.CaHash, result.Error);
                //     return;
                // }

                await _graphQLProvider.SetLastEndHeightAsync(chainId, queryType, dto.BlockHeight);
                _logger.LogInformation("{type} SyncToMain succeed on chain: {id} of account: {hash}", dto.ChangeType,
                    chainInfo.ChainId, dto.CaHash);
            }
        }
    }

    private async Task QueryLoginGuardianEventsAndSyncAsync(string chainId)
    {
        try
        {
            var lastEndHeight = await _graphQLProvider.GetLastEndHeightAsync(chainId, QueryType.LoginGuardian);
            var currentIndexHeight = await _graphQLProvider.GetIndexBlockHeightAsync(chainId);
            var endBlockHeight = GetEndBlockHeight(lastEndHeight, _indexOptions.IndexInterval, currentIndexHeight);

            if (endBlockHeight == ContractAppServiceConstant.LongError)
            {
                _logger.LogWarning(
                    "QueryLoginGuardianEventsAndSync on chain: {id}. Index Height is not enough. Skipped querying this time. \nLastEndHeight: {last}, CurrentIndexHeight: {index}",
                    chainId, lastEndHeight, currentIndexHeight);
                return;
            }

            var queryEvents = await _graphQLProvider.GetLoginGuardianTransactionInfosAsync(
                chainId, lastEndHeight + 1, endBlockHeight);

            if (queryEvents.IsNullOrEmpty())
            {
                var nextIndexHeight = endBlockHeight < currentIndexHeight
                    ? endBlockHeight + 1
                    : currentIndexHeight - _indexOptions.IndexSafe;

                await _graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.LoginGuardian, nextIndexHeight);

                _logger.LogInformation(
                    "Found no LoginGuardian events on chain: {id}. Next index block height: {height}", chainId,
                    nextIndexHeight);
                return;
            }

            queryEvents = OptimizeQueryEvents(queryEvents);
            _logger.LogInformation(
                "Found {num} LoginGuardian events on chain: {id}", queryEvents.Count, chainId);


            await ValidateQueryEventsAndSyncAsync(chainId, queryEvents, QueryType.LoginGuardian);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "QueryLoginGuardianEventsAndSync on chain: {id} Error", chainId);
        }
    }

    private async Task QueryManagerEventsAndSyncAsync(string chainId)
    {
        try
        {
            var lastEndHeight = await _graphQLProvider.GetLastEndHeightAsync(chainId, QueryType.ManagerInfo);
            var currentIndexHeight = await _graphQLProvider.GetIndexBlockHeightAsync(chainId);
            var endBlockHeight = GetEndBlockHeight(lastEndHeight, _indexOptions.IndexInterval, currentIndexHeight);

            if (endBlockHeight == ContractAppServiceConstant.LongError)
            {
                _logger.LogWarning(
                    "QueryManagerEventsAndSyncAsync on chain: {id}. Index Height is not enough. Skipped querying this time. \nLastEndHeight: {last}, CurrentIndexHeight: {index}",
                    chainId, lastEndHeight, currentIndexHeight);
                return;
            }

            var queryEvents =
                await _graphQLProvider.GetManagerTransactionInfosAsync(chainId, lastEndHeight + 1, endBlockHeight);

            if (queryEvents.IsNullOrEmpty())
            {
                var nextIndexHeight = endBlockHeight < currentIndexHeight
                    ? endBlockHeight + 1
                    : currentIndexHeight - _indexOptions.IndexSafe;
                await _graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.ManagerInfo, nextIndexHeight);

                _logger.LogInformation("Found no Manager events on chain: {id}. Next index block height: {height}",
                    chainId, nextIndexHeight);
                return;
            }

            queryEvents = OptimizeQueryEvents(queryEvents);
            _logger.LogInformation("Found {num} Manager events on chain: {id}", queryEvents.Count, chainId);

            await ValidateQueryEventsAndSyncAsync(chainId, queryEvents, QueryType.ManagerInfo);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "QueryManagerEventsAndSync on chain: {id} error", chainId);
        }
    }

    private async Task ValidateQueryEventsAndSyncAsync(string chainId, List<QueryEventDto> queryEvents,
        string queryType)
    {
        var dict = new Dictionary<QueryEventDto, SyncHolderInfoInput>();
        var lastChainHeight = ContractAppServiceConstant.LongEmpty;

        try
        {
            foreach (var dto in queryEvents)
            {
                _logger.LogInformation(
                    "Event type: {type} sync starting on chain: {id} of account: {hash} at Height: {height}",
                    dto.ChangeType, chainId, dto.CaHash, dto.BlockHeight);

                var currentBlockHeight = await _contractProvider.GetBlockHeightAsync(chainId);

                if (currentBlockHeight <= dto.BlockHeight)
                {
                    _logger.LogWarning(LoggerMsg.NodeBlockHeightWarning);
                    break;
                }

                var unsetLoginGuardians = new RepeatedField<string>();

                switch (dto.ChangeType)
                {
                    case QueryLoginGuardianType.LoginGuardianAdded:
                        unsetLoginGuardians = null;
                        break;
                    case QueryLoginGuardianType.LoginGuardianUnbound:
                        unsetLoginGuardians.Add(dto.Value);
                        break;
                }

                var outputGetHolderInfo =
                    await _contractProvider.GetHolderInfoFromChainAsync(chainId, Hash.Empty, dto.CaHash);
                var transactionDto =
                    await _contractProvider.ValidateTransactionAsync(chainId, outputGetHolderInfo,
                        unsetLoginGuardians);

                if (transactionDto.TransactionResultDto.Status != TransactionState.Mined)
                {
                    break;
                }

                var syncHolderInfoInput =
                    await _contractProvider.GetSyncHolderInfoInputAsync(chainId, transactionDto);

                if (syncHolderInfoInput.VerificationTransactionInfo == null)
                {
                    continue;
                }

                lastChainHeight = syncHolderInfoInput.VerificationTransactionInfo.ParentChainHeight;
                dict.Add(dto, syncHolderInfoInput);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ValidateQueryEventsAndSync error");
        }
        finally
        {
            if (dict.Count != 0)
            {
                await SyncQueryEventsAsync(chainId, lastChainHeight, dict, queryType);
            }
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
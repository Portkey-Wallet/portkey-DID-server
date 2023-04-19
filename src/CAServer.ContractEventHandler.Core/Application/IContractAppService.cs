using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Etos;
using CAServer.Grains.Grain.ApplicationHandler;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IContractAppService
{
    public Task CreateHolderInfoAsync(AccountRegisterCreateEto message);
    public Task SocialRecoveryAsync(AccountRecoverCreateEto message);
    public Task QueryEventsAndSyncAsync();
    public Task InitializeIndexAsync();
    public Task InitializeIndexAsync(long blockHeight);
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
        IOptions<IndexOptions> contractOptions, IGraphQLProvider graphQLProvider, IContractProvider contractProvider,
        IObjectMapper objectMapper, ILogger<ContractAppService> logger)
    {
        _distributedEventBus = distributedEventBus;
        _indexOptions = contractOptions.Value;
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

        var checkCaHolderExists = await CheckHolderExistsOnBothChains(createHolderDto.GuardianAccountInfo.Value);

        if (!checkCaHolderExists)
        {
            registerResult.RegisterMessage = "LoginGuardianAccount: " + createHolderDto.GuardianAccountInfo.Value +
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
                createHolderDto.GuardianAccountInfo.Value, null);

        if (outputGetHolderInfo.CaHash.IsNullOrEmpty())
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

        await ValidateTransactionAndSyncAsync(createHolderDto.ChainId, outputGetHolderInfo, "");
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
            socialRecoveryDto.LoginGuardianAccount, null);

        if (outputGetHolderInfo.CaHash.IsNullOrEmpty())
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

        await ValidateTransactionAndSyncAsync(socialRecoveryDto.ChainId, outputGetHolderInfo, "");
    }

    private async Task<bool> ValidateTransactionAndSyncAsync(string chainId, GetHolderInfoOutput result,
        string optionChainId)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        var transactionDto =
            await _contractProvider.ValidateTransactionAsync(chainId, result, null);
        var syncHolderInfoInput = await _contractProvider.GetSyncHolderInfoInputAsync(chainId, transactionDto);

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
                await SideChainCheckMainChainBlockIndex(sideChain.ChainId,
                    syncHolderInfoInput.VerificationTransactionInfo.ParentChainHeight);

                var resultDto = await _contractProvider.SyncTransactionAsync(sideChain.ChainId, syncHolderInfoInput);
                syncSucceed = syncSucceed && resultDto.Status == TransactionState.Mined;
            }
        }
        else
        {
            var mainChain = _chainOptions.ChainInfos.Values.First(c => c.IsMainChain);
            await MainChainCheckSideChainBlockIndex(chainId,
                syncHolderInfoInput.VerificationTransactionInfo.ParentChainHeight);

            await _contractProvider.SyncTransactionAsync(mainChain.ChainId, syncHolderInfoInput);

            syncSucceed = await ValidateTransactionAndSyncAsync(mainChain.ChainId, result, chainId);
        }

        return syncSucceed;
    }

    private async Task MainChainCheckSideChainBlockIndex(string chainIdSide, long txHeight)
    {
        var mainHeight = long.MaxValue;
        var checkResult = false;
        var sideChainIdInInt = await _contractProvider.GetChainIdAsync(chainIdSide);

        var mainChain = _chainOptions.ChainInfos.Values.First(c => c.IsMainChain);

        while (!checkResult)
        {
            var indexSideChainBlock =
                await _contractProvider.GetIndexHeightFromMainChainAsync(mainChain.ChainId, sideChainIdInInt);

            if (indexSideChainBlock < txHeight)
            {
                _logger.LogInformation("Block is not recorded, waiting...");
                await Task.Delay(_indexOptions.IndexDelay);
                continue;
            }

            mainHeight = mainHeight == long.MaxValue
                ? await _contractProvider.GetBlockHeightAsync(mainChain.ChainId)
                : mainHeight;

            var indexMainChainBlock = await _contractProvider.GetIndexHeightFromSideChainAsync(chainIdSide);
            checkResult = indexMainChainBlock > mainHeight;
        }
    }

    private async Task SideChainCheckMainChainBlockIndex(string chainIdSide, long txHeight)
    {
        var checkResult = false;

        while (!checkResult)
        {
            var indexMainChainBlock = await _contractProvider.GetIndexHeightFromSideChainAsync(chainIdSide);

            if (indexMainChainBlock < txHeight)
            {
                _logger.LogInformation("Block is not recorded, waiting...");
                await Task.Delay(_indexOptions.IndexDelay);
                continue;
            }

            checkResult = true;
        }
    }

    private async Task<bool> CheckHolderExistsOnBothChains(string loginGuardianAccount)
    {
        var tasks = new List<Task<bool>>();
        foreach (var chainId in _chainOptions.ChainInfos.Keys)
        {
            tasks.Add(CheckHolderExists(chainId, loginGuardianAccount));
        }

        var results = await tasks.WhenAll();

        return results.All(r => r == false);
    }

    private async Task<bool> CheckHolderExists(string chainId, string loginGuardianAccount)
    {
        var outputMain = await _contractProvider.GetHolderInfoFromChainAsync(chainId, loginGuardianAccount, null);
        if (outputMain.CaHash.IsNullOrEmpty())
        {
            return false;
        }

        _logger.LogInformation("LoginGuardianAccount: {loginGuardianAccount} on chain {id} is occupied",
            loginGuardianAccount, chainId);
        return true;
    }

    public async Task QueryEventsAndSyncAsync()
    {
        foreach (var chainId in _chainOptions.ChainInfos.Keys)
        {
            _logger.LogInformation("QueryEventsAndSync on chain: {id} Starts", chainId);
            await QueryLoginGuardianAccountEventsAndSyncAsync(chainId);
            await QueryManagerEventsAndSyncAsync(chainId);
        }
    }

    private long GetEndBlockHeight(long lastEndHeight, long interval, long currentIndexHeight)
    {
        if (lastEndHeight + 1 >= currentIndexHeight - _indexOptions.IndexSafe)
        {
            return ContractAppServiceConstant.LongError;
        }

        var height = lastEndHeight + 1 + interval;
        var height2 = currentIndexHeight - _indexOptions.IndexSafe;

        return height < height2 ? height : height2;
    }

    private async Task SyncQueryEventsAsync(string chainId, long lastChainHeight,
        Dictionary<QueryEventDto, SyncHolderInfoInput> dict, string queryType)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        if (chainInfo.IsMainChain)
        {
            foreach (var info in _chainOptions.ChainInfos.Values.Where(info => !info.IsMainChain))
            {
                await SideChainCheckMainChainBlockIndex(info.ChainId, lastChainHeight);

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
                        dto.ChangeType,
                        chainInfo.ChainId, dto.CaHash);
                }
            }
        }
        else
        {
            await MainChainCheckSideChainBlockIndex(chainId, lastChainHeight);
            var mainChain = _chainOptions.ChainInfos.Values.First(c => c.IsMainChain);

            foreach (var dto in dict.Keys)
            {
                var result =
                    await _contractProvider.SyncTransactionAsync(mainChain.ChainId, dict[dto]);
                if (result.Status != TransactionState.Mined)
                {
                    _logger.LogError("{type} SyncToMain failed on chain: {id} of account: {hash}, error: {error}",
                        dto.ChangeType, mainChain.ChainId, dto.CaHash, result.Error);
                    return;
                }

                var output = await _contractProvider.GetHolderInfoFromChainAsync(mainChain.ChainId, "", dto.CaHash);

                var syncResult = await ValidateTransactionAndSyncAsync(mainChain.ChainId, output, chainId);

                if (!syncResult)
                {
                    _logger.LogError("{type} SyncToOthers failed on chain: {id} of account: {hash}, error: {error}",
                        dto.ChangeType, chainInfo.ChainId, dto.CaHash, result.Error);
                    return;
                }

                await _graphQLProvider.SetLastEndHeightAsync(chainId, queryType, dto.BlockHeight);
                _logger.LogInformation("{type} SyncToMain succeed on chain: {id} of account: {hash}", dto.ChangeType,
                    chainInfo.ChainId, dto.CaHash);
            }
        }
    }

    private async Task QueryLoginGuardianAccountEventsAndSyncAsync(string chainId)
    {
        try
        {
            var lastEndHeight = await _graphQLProvider.GetLastEndHeightAsync(chainId, QueryType.LoginGuardianAccount);
            var currentIndexHeight = await _graphQLProvider.GetIndexBlockHeightAsync(chainId);
            var endBlockHeight = GetEndBlockHeight(lastEndHeight, _indexOptions.IndexInterval, currentIndexHeight);

            if (endBlockHeight == ContractAppServiceConstant.LongError)
            {
                _logger.LogWarning(
                    "QueryLoginGuardianAccountEventsAndSync on chain: {id}. Index Height is not enough. Skipped querying this time. \nLastEndHeight: {last}, CurrentIndexHeight: {index}",
                    chainId, lastEndHeight, currentIndexHeight);
                return;
            }

            var queryEvents = await _graphQLProvider.GetLoginGuardianAccountTransactionInfosAsync(
                chainId, lastEndHeight + 1, endBlockHeight);

            if (queryEvents.IsNullOrEmpty())
            {
                var nextIndexHeight = endBlockHeight < currentIndexHeight
                    ? endBlockHeight + 1
                    : currentIndexHeight - _indexOptions.IndexSafe;

                await _graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.LoginGuardianAccount, nextIndexHeight);

                _logger.LogInformation(
                    "Found no LoginGuardianAccount events on chain: {id}. Next index block height: {height}", chainId,
                    nextIndexHeight);
                return;
            }

            queryEvents = OptimizeQueryEvents(queryEvents);
            _logger.LogInformation(
                "Found {num} LoginGuardianAccount events on chain: {id}", queryEvents.Count, chainId);


            await ValidateQueryEventsAndSyncAsync(chainId, queryEvents, QueryType.LoginGuardianAccount);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "QueryLoginGuardianAccountEventsAndSync on chain: {id} Error", chainId);
        }
    }

    private async Task QueryManagerEventsAndSyncAsync(string chainId)
    {
        try
        {
            var lastEndHeight = await _graphQLProvider.GetLastEndHeightAsync(chainId, QueryType.Manager);
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
                await _graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.Manager, nextIndexHeight);

                _logger.LogInformation("Found no Manager events on chain: {id}. Next index block height: {height}",
                    chainId, nextIndexHeight);
                return;
            }

            queryEvents = OptimizeQueryEvents(queryEvents);
            _logger.LogInformation("Found {num} Manager events on chain: {id}", queryEvents.Count, chainId);

            await ValidateQueryEventsAndSyncAsync(chainId, queryEvents, QueryType.Manager);
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
                    _logger.LogWarning("Current node block height should be large than the event");
                    break;
                }

                var unsetLoginGuardianAccounts = new RepeatedField<string>();
                switch (dto.ChangeType)
                {
                    case QueryLoginGuardianAccountType.LoginGuardianAccountAdded:
                        unsetLoginGuardianAccounts = null;
                        break;
                    case QueryLoginGuardianAccountType.LoginGuardianAccountUnbound:
                        unsetLoginGuardianAccounts.Add(dto.Value);
                        break;
                }

                var outputGetHolderInfo =
                    await _contractProvider.GetHolderInfoFromChainAsync(chainId, "", dto.CaHash);
                var transactionDto =
                    await _contractProvider.ValidateTransactionAsync(chainId, outputGetHolderInfo,
                        unsetLoginGuardianAccounts);

                if (transactionDto.TransactionResultDto.Status != TransactionState.Mined)
                {
                    break;
                }

                var syncHolderInfoInput = await _contractProvider.GetSyncHolderInfoInputAsync(chainId, transactionDto);

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
                e.ChangeType != QueryLoginGuardianAccountType.LoginGuardianAccountUnbound &&
                e.CaHash == queryEvents[index].CaHash &&
                e.BlockHeight < queryEvents[index].BlockHeight);
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
            var height = await _graphQLProvider.GetLastEndHeightAsync(chainId, QueryType.LoginGuardianAccount);
            var height1 = await _graphQLProvider.GetLastEndHeightAsync(chainId, QueryType.Manager);

            var indexHeight = await _graphQLProvider.GetIndexBlockHeightAsync(chainId);
            if (height == 0)
            {
                tasks.Add(_graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.LoginGuardianAccount,
                    indexHeight - _indexOptions.IndexSafe));
            }

            if (height1 == 0)
            {
                tasks.Add(_graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.Manager,
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
            tasks.Add(_graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.LoginGuardianAccount, blockHeight));
            tasks.Add(_graphQLProvider.SetLastEndHeightAsync(chainId, QueryType.Manager, blockHeight));
        }

        await tasks.WhenAll();
    }
}
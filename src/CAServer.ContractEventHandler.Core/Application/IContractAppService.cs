using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Types;
using CAServer.Commons;
using CAServer.Etos;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.ValidateOriginChainId;
using CAServer.Grains.Grain.RedPackage;
using CAServer.Grains.State.ApplicationHandler;
using CAServer.Guardian.Provider;
using CAServer.Monitor;
using CAServer.Monitor.Logger;
using CAServer.UserAssets.Provider;
using CAServer.RedPackage.Etos;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Orleans;
using Orleans.Runtime;
using Portkey.Contracts.CA;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using GuardianInfo = Portkey.Contracts.CA.GuardianInfo;
using ManagerInfo = CAServer.Account.ManagerInfo;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IContractAppService : ISingletonDependency
{
    Task CreateRedPackageAsync(RedPackageCreateEto message);
    Task CreateHolderInfoAsync(AccountRegisterCreateEto message);
    Task SocialRecoveryAsync(AccountRecoverCreateEto message);
    Task QueryAndSyncAsync();

    Task InitializeIndexAsync();

    Task SyncOriginChainIdAsync(UserLoginEto userLoginEto);

    // Task UpdateOriginChainIdAsync(string originChainId, string syncChainId ,UserLoginEto userLoginEto);
    // Task InitializeQueryRecordIndexAsync();
    // Task InitializeIndexAsync(long blockHeight);

    Task<bool> RefundAsync(Guid redPackageId);
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
    private readonly IGuardianProvider _guardianProvider;
    private readonly IClusterClient _clusterClient;
    private readonly SyncOriginChainIdOptions _syncOriginChainIdOptions;
    private readonly IUserAssetsProvider _userAssetsProvider;
    private readonly PayRedPackageAccount _packageAccount;
    private readonly IRedPackageCreateResultService _redPackageCreateResultService;
    private const int AcceleratedThreadCount = 3;

    public ContractAppService(IDistributedEventBus distributedEventBus, IOptionsSnapshot<ChainOptions> chainOptions,
        IOptionsSnapshot<IndexOptions> indexOptions, IGraphQLProvider graphQLProvider,
        IContractProvider contractProvider, IObjectMapper objectMapper, ILogger<ContractAppService> logger,
        IRecordsBucketContainer recordsBucketContainer, IIndicatorLogger indicatorLogger,
        IGuardianProvider guardianProvider, IClusterClient clusterClient,
        IOptions<SyncOriginChainIdOptions> syncOriginChainIdOptions,
        IUserAssetsProvider userAssetsProvider,
        IMonitorLogProvider monitorLogProvider, IDistributedCache<string> distributedCache,
        IOptionsSnapshot<PayRedPackageAccount> packageAccount,
        IRedPackageCreateResultService redPackageCreateResultService)
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
        _guardianProvider = guardianProvider;
        _clusterClient = clusterClient;
        _syncOriginChainIdOptions = syncOriginChainIdOptions.Value;
        _userAssetsProvider = userAssetsProvider;
        _packageAccount = packageAccount.Value;
        _redPackageCreateResultService = redPackageCreateResultService;
    }

    public async Task CreateRedPackageAsync(RedPackageCreateEto eventData)
    {
        _logger.LogInformation("CreateRedPackage message: " + "\n{message}",
            JsonConvert.SerializeObject(eventData, Formatting.Indented));

        var eto = new RedPackageCreateResultEto();
        eto.SessionId = eventData.SessionId;
        try
        {
            var result = await _contractProvider.ForwardTransactionAsync(eventData.ChainId, eventData.RawTransaction);
            _logger.LogInformation("RedPackageCreate result: " + "\n{result}",
                JsonConvert.SerializeObject(result, Formatting.Indented));
            eto.TransactionResult = result.Status;
            eto.TransactionId = result.TransactionId;
            if (result.Status != TransactionState.Mined)
            {
                eto.Message = "Transaction status: " + result.Status + ". Error: " +
                              result.Error;
                eto.Success = false;

                _logger.LogInformation("RedPackageCreate pushed: " + "\n{result}",
                    JsonConvert.SerializeObject(eto, Formatting.Indented));

                _ = _redPackageCreateResultService.UpdateRedPackageAndSendMessageAsync(eto);
                return;
            }

            if (!result.Logs.Select(l => l.Name).Contains(LogEvent.CryptoBoxCreated))
            {
                eto.Message = "Transaction status: FAILED" + ". Error: Verification failed";
                eto.Success = false;

                _logger.LogInformation("RedPackageCreate pushed: " + "\n{result}",
                    JsonConvert.SerializeObject(eto, Formatting.Indented));

                _ = _redPackageCreateResultService.UpdateRedPackageAndSendMessageAsync(eto);
                return;
            }

            eto.Success = true;
            eto.Message = "Transaction status: " + result.Status;
            _ = _redPackageCreateResultService.UpdateRedPackageAndSendMessageAsync(eto);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "RedPackageCreateEto Error: user:{user},sessionId:{session}",
                eventData.UserId?.ToString(),
                eventData.SessionId.ToString());
            eto.Success = false;
            eto.Message = e.Message;
            _ = _redPackageCreateResultService.UpdateRedPackageAndSendMessageAsync(eto);
        }
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
            await _distributedEventBus.PublishAsync(registerResult);

            return;
        }

        //Speed up registration
        _ = CreateHolderInfoOnNonCreateChainAsync(outputGetHolderInfo, createHolderDto);

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

        var managerInfoExisted = resultSocialRecovery.Status == TransactionState.NodeValidationFailed &&
                                 resultSocialRecovery.Error.Contains("ManagerInfo exists");
        if (resultSocialRecovery.Status != TransactionState.Mined && !managerInfoExisted)
        {
            recoveryResult.RecoveryMessage = "Transaction status: " + resultSocialRecovery.Status + ". Error: " +
                                             resultSocialRecovery.Error;
            recoveryResult.RecoverySuccess = false;

            _logger.LogInformation("Recovery state pushed: " + "\n{result}",
                JsonConvert.SerializeObject(recoveryResult, Formatting.Indented));

            await _distributedEventBus.PublishAsync(recoveryResult);

            return;
        }

        if (!managerInfoExisted &&
            !resultSocialRecovery.Logs.Select(l => l.Name).Contains(LogEvent.ManagerInfoSocialRecovered))
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
            await _distributedEventBus.PublishAsync(recoveryResult);
            return;
        }

        var originalChainId = socialRecoveryDto.ChainId;
        _ = SocialRecoveryOnNonCreateChainAsync(socialRecoveryDto);

        recoveryResult.RecoverySuccess = true;
        recoveryResult.CaHash = outputGetHolderInfo.CaHash.ToHex();
        recoveryResult.CaAddress = outputGetHolderInfo.CaAddress.ToBase58();

        await _distributedEventBus.PublishAsync(recoveryResult);

        _logger.LogInformation("Recovery state pushed: " + "\n{result}",
            JsonConvert.SerializeObject(recoveryResult, Formatting.Indented));

        // _logger.LogInformation("ValidateTransactionAndSyncAsync, holderInfo: {holderInfo}",
        //     JsonConvert.SerializeObject(outputGetHolderInfo));
        // ValidateAndSync can be very time consuming, so don't wait for it to finish
        // if the social recovery is executed on 'NonCreateChain', there is no synchronization
        if (originalChainId == ChainHelper.ConvertChainIdToBase58(outputGetHolderInfo.CreateChainId))
        {
            _ = ValidateTransactionAndSyncAsync(originalChainId, outputGetHolderInfo, "",
                MonitorTag.SocialRecover);
        }
        else
        {
            _logger.LogInformation("social recovery is executed on 'NonCreateChain', IdentifierHash={0}, CaHash={1}",
                socialRecoveryDto.LoginGuardianIdentifierHash?.ToHex(), socialRecoveryDto.CaHash?.ToHex());
        }
    }

    public async Task SyncOriginChainIdAsync(UserLoginEto userLoginEto)
    {
        if (!await NeedSyncStatusAsync(userLoginEto.UserId))
        {
            return;
        }

        var originChainId = "";
        var syncChainId = "";
        var guardians = await _guardianProvider.GetGuardiansAsync("", userLoginEto.CaHash);
        if (guardians == null || guardians.CaHolderInfo == null || guardians.CaHolderInfo.Count == 0)
        {
            _logger.LogInformation("CheckOriginChainIdStatusAsync fail,guardians is null or empty,userId {uid}",
                userLoginEto.UserId);
            return;
        }

        originChainId = guardians.CaHolderInfo?.FirstOrDefault()?.OriginChainId;
        if (string.IsNullOrWhiteSpace(originChainId))
        {
            _logger.LogInformation("CheckOriginChainIdStatusAsync fail,originChainId is null or empty,userId {uid}",
                userLoginEto.UserId);
            return;
        }

        syncChainId = _chainOptions.ChainInfos.Where(kvp => kvp.Key != originChainId).Select(kvp => kvp.Key)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(syncChainId))
        {
            _logger.LogInformation("CheckOriginChainIdStatusAsync fail,syncChainId is null or empty,userId {uid}",
                userLoginEto.UserId);
            return;
        }

        //this will take very long time
        await UpdateOriginChainIdAsync(originChainId, syncChainId, userLoginEto);
    }

    public async Task UpdateOriginChainIdAsync(string originChainId, string syncChainId, UserLoginEto userLoginEto)
    {
        var validateOriginChainIdGrain = _clusterClient.GetGrain<IValidateOriginChainIdGrain>(userLoginEto.UserId);
        try
        {
            var needValidate = await validateOriginChainIdGrain.NeedValidateAsync();
            _logger.LogInformation(
                "UpdateOriginChainIdAsync,needValidate {needValidate},cahash:{cahash},uid:{uid} ,originChainId:{originChainId}",
                needValidate.Data, userLoginEto.CaHash, userLoginEto.UserId, originChainId);

            if (!needValidate.Data)
            {
                return;
            }

            var holderInfoOutput =
                await _contractProvider.GetHolderInfoFromChainAsync(originChainId, null, userLoginEto.CaHash);

            var syncHolderInfoOutput =
                await _contractProvider.GetHolderInfoFromChainAsync(syncChainId, null, userLoginEto.CaHash);

            if (holderInfoOutput.CreateChainId > 0 && syncHolderInfoOutput.CreateChainId > 0)
            {
                await validateOriginChainIdGrain.SetStatusSuccessAsync();
                _logger.LogInformation(
                    "UpdateOriginChainIdAsync already success,chainId {chainId},userId {uid}", originChainId,
                    userLoginEto.UserId);
                return;
            }

            holderInfoOutput.CreateChainId = ChainHelper.ConvertBase58ToChainId(originChainId);


            await validateOriginChainIdGrain.SetStatusSuccessAsync();
            _logger.LogInformation(
                "UpdateOriginChainIdAsync success,originChainId {originChainId}:{holderInfoOutput.CreateChainId}, syncChainId:{syncChainId}:{syncHolderInfoOutput.CreateChainId},userId {uid}",
                originChainId, holderInfoOutput.CreateChainId, syncChainId, syncHolderInfoOutput.CreateChainId,
                userLoginEto.UserId);
            _ = ValidateTransactionAndSyncAsync(originChainId, holderInfoOutput, "",
                MonitorTag.LoginSync);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UpdateOriginChainIdAsync fail,chainId {chainId},userId {uid},cahash:{cahash}",
                originChainId,
                userLoginEto.UserId, userLoginEto.CaHash);
            await validateOriginChainIdGrain.SetStatusFailAsync();
        }
    }

    public async Task<bool> NeedSyncStatusAsync(Guid userId)
    {
        var caHolderIndex = await _userAssetsProvider.GetCaHolderIndexAsync(userId);
        if (caHolderIndex == null || caHolderIndex.IsDeleted)
        {
            _logger.LogInformation("UpdateOriginChainIdAsync caHolderIndex is null or deleted,userId {uid}", userId);
            return false;
        }

        _logger.LogInformation(
            "UpdateOriginChainIdAsync caHolderIndex.CreateTime:{caHolderIndex.CreateTime},checkTime:{time}",
            (TimeHelper.GetTimeStampFromDateTime(caHolderIndex.CreateTime),
                _syncOriginChainIdOptions.CheckUserRegistrationTimestamp));

        if (TimeHelper.GetTimeStampFromDateTime(caHolderIndex.CreateTime) >
            _syncOriginChainIdOptions.CheckUserRegistrationTimestamp)
        {
            return false;
        }

        return true;
    }

    public async Task<bool> RefundAsync(Guid redPackageId)
    {
        try
        {
            var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageId);

            var redPackageDetail = await grain.GetRedPackage(redPackageId);
            var redPackageDetailDto = redPackageDetail.Data;
            var payRedPackageFrom = _packageAccount.getOneAccountRandom();
            _logger.Info("Refund red package payRedPackageFrom,payRedPackageFrom:{payRedPackageFrom} ",
                payRedPackageFrom);

            if (redPackageDetailDto.Status.Equals(RedPackageStatus.Expired) &&
                !redPackageDetailDto.IsRedPackageFullyClaimed)
            {
                var res = await _contractProvider.SendTransferRedPacketRefundAsync(redPackageDetailDto,
                    payRedPackageFrom);
                if (res.TransactionResultDto.Status == TransactionState.Mined)
                {
                    await grain.UpdateRedPackageExpire();
                    return true;
                }

                _logger.LogError("Refund red package fail {message}", res.TransactionResultDto.Error);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Refund red package fail {message}", e.Message);
        }

        return false;
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
        var unsetLoginGuardians = new RepeatedField<string>();
        foreach (var guardian in result.GuardianList.Guardians)
        {
            if (!guardian.IsLoginGuardian)
            {
                unsetLoginGuardians.Add(guardian.IdentifierHash.ToHex());
            }
        }

        var transactionDto =
            await _contractProvider.ValidateTransactionAsync(chainId, result, unsetLoginGuardians);
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
            if (!await CheckSyncHolderVersionAsync(ContractAppServiceConstant.MainChainId, result.CaHash.ToHex(),
                    validateHeight))
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
                await UpdateSyncHolderVersionAsync(ContractAppServiceConstant.MainChainId, result.CaHash.ToHex(),
                    validateHeight);
            }
        }

        return syncSucceed;
    }

    private async Task<bool> CheckHolderExistsOnBothChains(Hash loginGuardian)
    {
        var tasks = new List<Task<Tuple<bool, bool>>>();
        foreach (var chainId in _chainOptions.ChainInfos.Keys)
        {
            tasks.Add(CheckHolderExists(chainId, loginGuardian));
        }

        var results = await tasks.WhenAll();
        //CAHolder does not exist or CAHolder can only be registered in NonCreateChain.
        return results.All(r => !r.Item1 || (r.Item1 && !r.Item2));
    }

    private async Task<Tuple<bool, bool>> CheckHolderExists(string chainId, Hash loginGuardian)
    {
        var outputMain = await _contractProvider.GetHolderInfoFromChainAsync(chainId, loginGuardian, null);
        if (outputMain.CaHash == null || outputMain.CaHash.Value.IsEmpty)
        {
            return new Tuple<bool, bool>(false, false);
        }


        var createChainId = ChainHelper.ConvertChainIdToBase58(outputMain.CreateChainId);

        _logger.LogInformation("LoginGuardian: {loginGuardian} on chain {id} is occupied",
            loginGuardian.ToHex(), chainId);
        return new Tuple<bool, bool>(true, chainId == createChainId);
    }

    private bool EnableAcceleration(List<GuardianInfo> guardianInfos)
    {
        if (guardianInfos == null || guardianInfos.Count == 0)
        {
            return false;
        }

        return guardianInfos.All(guardianInfo => guardianInfo?.VerificationInfo != null &&
                                                 !guardianInfo.VerificationInfo.VerificationDoc.IsNullOrWhiteSpace() &&
                                                 GetVerificationDocLength(guardianInfo.VerificationInfo
                                                     .VerificationDoc) >= 8);
    }

    private int GetVerificationDocLength(string verificationDoc)
    {
        return string.IsNullOrWhiteSpace(verificationDoc) ? 0 : verificationDoc.Split(",").Length;
    }

    private async Task CreateHolderInfoOnNonCreateChainAsync(
        GetHolderInfoOutput outputGetHolderInfo,
        CreateHolderDto createHolderDto)
    {
        var watcher = Stopwatch.StartNew();
        try
        {
            var list = new List<GuardianInfo> { createHolderDto.GuardianInfo };
            if (!EnableAcceleration(list))
            {
                _logger.LogWarning("CreateHolderInfo, OperationDetails is not signed in，caHash = {0}",
                    outputGetHolderInfo.CaHash?.ToHex());
                return;
            }

            var createChainId = createHolderDto.ChainId;
            var chainInfos = _chainOptions.ChainInfos.Values.Where(
                c => c.ChainId != createChainId);

            using var semaphore = new SemaphoreSlim(AcceleratedThreadCount);
            var tasks = chainInfos.Select(async chain =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var result = await _contractProvider.CreateHolderInfoOnNonCreateChainAsync(chain,
                            outputGetHolderInfo,
                            createHolderDto);
                        _logger.LogInformation("accelerate registration: {0}, transactionId:{1}",
                            JsonConvert.SerializeObject(createHolderDto), result.TransactionId);

                        await SendCreateHolderInfoOnNonCreateChainMessageAsync(chain, createHolderDto,
                            outputGetHolderInfo,
                            result);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "create HolderInfo on 'NonCreateChain' error, caHash = {0}",
                            outputGetHolderInfo.CaHash?.ToHex());
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                })
                .ToList();
            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "accelerated registration error: CaHash={caHash}", outputGetHolderInfo.CaHash);
        }
        finally
        {
            watcher.Stop();
            _indicatorLogger.LogInformation(MonitorTag.Accelerate, Enum.GetName(MonitorTag.Accelerate) ?? string.Empty,
                (int)watcher.ElapsedMilliseconds);
        }
    }

    private async Task SendCreateHolderInfoOnNonCreateChainMessageAsync(ChainInfo chainInfo,
        CreateHolderDto createHolderDto, GetHolderInfoOutput outputGetHolderInfo,
        TransactionResultDto transactionResultDto
    )
    {
        var registerResult = new AccelerateCreateHolderEto
        {
            Id = createHolderDto.Id,
            CaAddress = outputGetHolderInfo.CaAddress.ToBase58(),
            CaHash = outputGetHolderInfo.CaHash.ToHex(),
            IdentifierHash = createHolderDto.GuardianInfo?.IdentifierHash?.ToHex(),
            ChainId = chainInfo.ChainId,
            ManagerInfo = new ManagerInfo
            {
                Address = createHolderDto.ManagerInfo.Address?.ToBase58(),
                ExtraData = createHolderDto.ManagerInfo.ExtraData
            },
            RegisterSuccess = true
        };

        if (transactionResultDto.Status != TransactionState.Mined)
        {
            registerResult.RegisterMessage = "Transaction status: " + transactionResultDto.Status + ". Error: " +
                                             transactionResultDto.Error;
            registerResult.RegisterSuccess = false;

            _logger.LogInformation("accelerated registration state: " + "\n{result}",
                JsonConvert.SerializeObject(registerResult, Formatting.Indented));
        }
        else if (!transactionResultDto.Logs.Select(l => l.Name).Contains(LogEvent.NonCreateChainCAHolderCreated))
        {
            registerResult.RegisterMessage = "Transaction status: FAILED" + ". Error: Verification failed";
            registerResult.RegisterSuccess = false;

            _logger.LogInformation("accelerated registration state: , id:{id}, grainId:{chainId}, message:{result}",
                registerResult.Id.ToString(), chainInfo.ChainId, registerResult.RegisterMessage);
        }

        await _distributedEventBus.PublishAsync(registerResult);

        _logger.LogInformation("accelerated registration state: " + "\n{result}",
            JsonConvert.SerializeObject(registerResult, Formatting.Indented));
    }

    private async Task SocialRecoveryOnNonCreateChainAsync(SocialRecoveryDto socialRecoveryDto)
    {
        var watcher = Stopwatch.StartNew();
        try
        {
            if (!EnableAcceleration(socialRecoveryDto.GuardianApproved))
            {
                _logger.LogWarning("SocialRecovery, OperationDetails is not signed in，identifierHash = {0}",
                    socialRecoveryDto.LoginGuardianIdentifierHash?.ToHex());
                return;
            }

            var chainId = socialRecoveryDto.ChainId;
            var chainInfos = _chainOptions.ChainInfos.Values.Where(c => c.ChainId != chainId);
            using var semaphore = new SemaphoreSlim(AcceleratedThreadCount);
            var tasks = chainInfos.Select(async chain =>
            {
                await semaphore.WaitAsync();
                try
                {
                    socialRecoveryDto.ChainId = chain.ChainId;
                    var transactionResultDto = await _contractProvider.SocialRecoveryAsync(socialRecoveryDto);
                    _logger.LogInformation("accelerate recovery: {0}, transactionId:{1}",
                        JsonConvert.SerializeObject(socialRecoveryDto), transactionResultDto.TransactionId);

                    await SendSocialRecoveryOnNonCreateChainMessageAsync(chain, socialRecoveryDto,
                        transactionResultDto);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "social recover on 'NonCreateChain' error, identifierHash = {0}",
                        socialRecoveryDto.LoginGuardianIdentifierHash?.ToHex());
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();
            await tasks.WhenAll();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "accelerated social recover error: CaHash={caHash}", socialRecoveryDto.CaHash?.ToHex());
        }
        finally
        {
            watcher.Stop();
            _indicatorLogger.LogInformation(MonitorTag.Accelerate,
                Enum.GetName(MonitorTag.AccelerateRecover) ?? string.Empty,
                (int)watcher.ElapsedMilliseconds);
        }
    }

    private async Task SendSocialRecoveryOnNonCreateChainMessageAsync(ChainInfo chainInfo,
        SocialRecoveryDto socialRecoveryDto, TransactionResultDto transactionResultDto)
    {
        var recoveryResult = new AccelerateSocialRecoveryEto
        {
            Id = socialRecoveryDto.Id,
            CaHash = socialRecoveryDto.CaHash?.ToHex(),
            CaAddress = socialRecoveryDto.CaAddress?.ToBase58(),
            IdentifierHash = socialRecoveryDto.LoginGuardianIdentifierHash?.ToHex(),
            ChainId = chainInfo.ChainId,
            ManagerInfo = new ManagerInfo
            {
                Address = socialRecoveryDto.ManagerInfo.Address?.ToBase58(),
                ExtraData = socialRecoveryDto.ManagerInfo.ExtraData
            },
            RecoverySuccess = true
        };

        if (transactionResultDto.Status != TransactionState.Mined)
        {
            recoveryResult.RecoveryMessage = "Transaction status: " + transactionResultDto.Status + ". Error: " +
                                             transactionResultDto.Error;
            recoveryResult.RecoverySuccess = false;

            _logger.LogInformation("accelerated social recover state: " + "\n{result}",
                JsonConvert.SerializeObject(recoveryResult, Formatting.Indented));
        }
        else if (!transactionResultDto.Logs.Select(l => l.Name).Contains(LogEvent.ManagerInfoSocialRecovered))
        {
            recoveryResult.RecoveryMessage = "Transaction status: FAILED" + ". Error: Verification failed";
            recoveryResult.RecoverySuccess = false;

            _logger.LogInformation("accelerated social recover state: id:{id}, chainId:{chainId}, message:{result}",
                recoveryResult.Id.ToString(), chainInfo.ChainId, recoveryResult.RecoveryMessage);
        }

        await _distributedEventBus.PublishAsync(recoveryResult);

        _logger.LogInformation("accelerated social recover state: " + "\n{result}",
            JsonConvert.SerializeObject(recoveryResult, Formatting.Indented));
    }

    public async Task QueryAndSyncAsync()
    {
        var tasks = _chainOptions.ChainInfos.Keys.Select(QueryEventsAndSyncAsync).ToList();
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
            var records = await _recordsBucketContainer.GetValidatedRecordsAsync(chainId);
            if (records.IsNullOrEmpty())
            {
                _logger.LogInformation("Found no record to sync on chain: {id}", chainId);
                return;
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
                    await _monitorLogProvider.AddHeightIndexMonitorLogAsync(chainId, indexHeight);

                    targetRecords = records.Where(r => r.ValidateHeight < indexHeight).ToList();

                    tasks.AddRange(targetRecords
                        .Select((record, index) => new { record, index })
                        .GroupBy(x => x.index / 20)
                        .Select(group => group.Select(x => x.record))
                        .Select(group => SyncRecordMainChainAsync(group.ToList(), info.ChainId))
                        .ToList());
                }
            }
            else
            {
                var indexHeight = await _contractProvider.GetIndexHeightFromMainChainAsync(
                    ContractAppServiceConstant.MainChainId, await _contractProvider.GetChainIdAsync(chainId));
                await _monitorLogProvider.AddHeightIndexMonitorLogAsync(chainId, indexHeight);

                targetRecords = records.Where(r => r.ValidateHeight < indexHeight).ToList();
                tasks.AddRange(targetRecords
                    .Select((record, index) => new { record, index })
                    .GroupBy(x => x.index / 20)
                    .Select(group => group.Select(x => x.record))
                    .Select(group => SyncRecordSideChainAsync(group.ToList(), chainId))
                    .ToList());
            }

            await tasks.WhenAll();
            var failedRecords = records.Where(r => r.ResultStatus == ResultStatus.NotMinded).ToList();
            await _recordsBucketContainer.AddToBeValidatedRecordsAsync(chainId, failedRecords);
            var leftRecords = records.Where(r => r.ResultStatus == ResultStatus.None).ToList();
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
            if (lastEndHeight == 0)
            {
                _logger.LogError(
                    "QueryEventsAsync on chain: {id}. Last End Height is 0. Skipped querying this time. \nLastEndHeight: {last}",
                    chainId, lastEndHeight);
                return;
            }

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
                        chainId, startIndexHeight, endIndexHeight),
                    _graphQLProvider.GetGuardianTransactionInfosAsync(
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

                var removeList = queryEvents.Where(eventDto =>
                        eventDto.ChangeType == QueryLoginGuardianType.LoginGuardianAdded && eventDto.IsCreateHolder)
                    .ToList();

                foreach (var removeEventDto in removeList)
                {
                    queryEvents.Remove(removeEventDto);
                }

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

            storedToBeValidatedRecords = storedToBeValidatedRecords
                .Where(r => r.BlockHeight >= _indexOptions.AutoSyncStartHeight[chainId]).ToList();

            storedToBeValidatedRecords = OptimizeSyncRecords(storedToBeValidatedRecords
                .Where(r => r.RetryTimes <= _indexOptions.MaxRetryTimes).ToList());

            foreach (var record in storedToBeValidatedRecords)
            {
                _logger.LogInformation(
                    "Event type: {type} validate starting on chain: {id} of account: {hash} at Height: {height}",
                    record.ChangeType, chainId, record.CaHash, record.BlockHeight);

                var currentBlockHeight = await _contractProvider.GetBlockHeightAsync(chainId);
                if (currentBlockHeight <= record.BlockHeight)
                {
                    _logger.LogWarning(LoggerMsg.NodeBlockHeightWarning);
                    break;
                }

                var outputGetHolderInfo =
                    await _contractProvider.GetHolderInfoFromChainAsync(chainId, Hash.Empty, record.CaHash);
                if (outputGetHolderInfo == null || outputGetHolderInfo.CaHash.IsNullOrEmpty())
                {
                    record.RetryTimes++;
                    failedRecords.Add(record);
                    continue;
                }

                var unsetLoginGuardians = new RepeatedField<string>();
                foreach (var guardian in outputGetHolderInfo.GuardianList.Guardians)
                {
                    if (!guardian.IsLoginGuardian)
                    {
                        unsetLoginGuardians.Add(guardian.IdentifierHash.ToHex());
                    }
                }

                var transactionDto =
                    await _contractProvider.ValidateTransactionAsync(chainId, outputGetHolderInfo,
                        unsetLoginGuardians);

                if (transactionDto.TransactionResultDto?.Status != TransactionState.Mined)
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
            _logger.LogInformation(
                "skip syncHolder targetChainId: {chainId}, caHash :{caHash},lastUpdateVersion:{version},curVersion:{curVersion}",
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

    private async Task SyncRecordMainChainAsync(List<SyncRecord> syncRecords, string chainId)
    {
        if (syncRecords.Count > 0)
        {
            foreach (var record in syncRecords)
            {
                if (!await CheckSyncHolderVersionAsync(chainId, record.CaHash, record.ValidateHeight))
                {
                    record.ResultStatus = ResultStatus.Synced;
                    continue;
                }
                _monitorLogProvider.AddNode(record, DataSyncType.BeginSync);
                var syncHolderInfoInput =
                    await _contractProvider.GetSyncHolderInfoInputAsync(chainId,
                        record.ValidateTransactionInfoDto);
                var result = await _contractProvider.SyncTransactionAsync(chainId, syncHolderInfoInput);
                if (result.Status != TransactionState.Mined)
                {
                    _logger.LogError(
                        "{type} SyncToSide failed on chain: {id} of account: {hash}, error: {error}, data:{data}",
                        record.ChangeType, chainId, record.CaHash, result.Error,
                        JsonConvert.SerializeObject(syncHolderInfoInput));

                    record.RetryTimes++;
                    record.ValidateHeight = long.MaxValue;
                    record.ValidateTransactionInfoDto = new TransactionInfo();
                    record.ResultStatus = ResultStatus.NotMinded;
                    if (result.Error.Contains("Already synced"))
                    {
                        record.ResultStatus = ResultStatus.Synced;
                    }
                }
                else
                {
                    await _monitorLogProvider.FinishAsync(record, chainId, result.BlockNumber);
                    await _monitorLogProvider.AddMonitorLogAsync(chainId, record.BlockHeight, chainId,
                        result.BlockNumber,
                        record.ChangeType);
                    _logger.LogInformation("{type} SyncToSide succeed on chain: {id} of account: {hash}",
                        record.ChangeType, chainId, record.CaHash);
                    await UpdateSyncHolderVersionAsync(chainId, record.CaHash, record.ValidateHeight);
                    record.ResultStatus = ResultStatus.Synced;
                }
            }
        }
    }

    private async Task SyncRecordSideChainAsync(List<SyncRecord> syncRecords, string chainId)
    {
        if (syncRecords.Count > 0)
        {
            foreach (var record in syncRecords)
            {
                if (!await CheckSyncHolderVersionAsync(ContractAppServiceConstant.MainChainId, record.CaHash,
                        record.ValidateHeight))
                {
                    record.ResultStatus = ResultStatus.Synced;
                    continue;
                }

                _monitorLogProvider.AddNode(record, DataSyncType.BeginSync);
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
                if (result.Status != TransactionState.Mined)
                {
                    _logger.LogError(
                        "{type} SyncToMain failed on chain: {id} of account: {hash}, error: {error}, data{data}",
                        record.ChangeType, chainId, record.CaHash, result.Error,
                        JsonConvert.SerializeObject(syncHolderInfoInput));

                    record.RetryTimes++;
                    record.ValidateHeight = long.MaxValue;
                    record.ValidateTransactionInfoDto = new TransactionInfo();
                    record.ResultStatus = ResultStatus.NotMinded;
                    if (result.Error.Contains("Already synced"))
                    {
                        record.ResultStatus = ResultStatus.Synced;
                    }
                }
                else
                {
                    await _monitorLogProvider.FinishAsync(record, ContractAppServiceConstant.MainChainId,
                        result.BlockNumber);
                    await _monitorLogProvider.AddMonitorLogAsync(chainId, record.BlockHeight,
                        ContractAppServiceConstant.MainChainId,
                        result.BlockNumber,
                        record.ChangeType);
                    await UpdateSyncHolderVersionAsync(ContractAppServiceConstant.MainChainId, record.CaHash,
                        record.ValidateHeight);
                    record.ResultStatus = ResultStatus.Synced;
                    _logger.LogInformation("{type} SyncToMain succeed on chain: {id} of account: {hash}",
                        record.ChangeType, chainId, record.CaHash);
                }
            }
        }
    }
}
﻿using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.ContractEventHandler;
using CAServer.Dtos;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.Device;
using CAServer.Hubs;
using CAServer.Monitor;
using CAServer.Monitor.Logger;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class CaAccountHandler : IDistributedEventHandler<AccountRegisterCreateEto>,
    IDistributedEventHandler<AccountRecoverCreateEto>,
    IDistributedEventHandler<CreateHolderEto>,
    IDistributedEventHandler<SocialRecoveryEto>,
    ITransientDependency
{
    private readonly INESTRepository<AccountRegisterIndex, Guid> _registerRepository;
    private readonly INESTRepository<AccountRecoverIndex, Guid> _recoverRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CaAccountHandler> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IIndicatorLogger _indicatorLogger;

    public CaAccountHandler(INESTRepository<AccountRegisterIndex, Guid> registerRepository,
        INESTRepository<AccountRecoverIndex, Guid> recoverRepository,
        IObjectMapper objectMapper,
        ILogger<CaAccountHandler> logger,
        IDistributedEventBus distributedEventBus,
        IClusterClient clusterClient,
        IIndicatorLogger indicatorLogger)
    {
        _registerRepository = registerRepository;
        _recoverRepository = recoverRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _indicatorLogger = indicatorLogger;
    }

    public async Task HandleEventAsync(AccountRegisterCreateEto eventData)
    {
        try
        {
            _logger.LogDebug("the first event: create register");
            var register = _objectMapper.Map<AccountRegisterCreateEto, AccountRegisterIndex>(eventData);

            register.RegisterStatus = AccountOperationStatus.Pending;
            await _registerRepository.AddAsync(register);

            _logger.LogDebug($"register add success: {JsonConvert.SerializeObject(register)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(AccountRecoverCreateEto eventData)
    {
        try
        {
            _logger.LogDebug("the first event: create recover");

            var recover = _objectMapper.Map<AccountRecoverCreateEto, AccountRecoverIndex>(eventData);

            recover.RecoveryStatus = AccountOperationStatus.Pending;
            await _recoverRepository.AddAsync(recover);

            _logger.LogDebug($"recovery add success: {JsonConvert.SerializeObject(recover)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(CreateHolderEto eventData)
    {
        try
        {
            _logger.LogDebug("the second event: update register grain.");

            await SwapGrainStateAsync(eventData.CaHash, eventData.GrainId);

            var grain = _clusterClient.GetGrain<IRegisterGrain>(eventData.GrainId);
            var result =
                await grain.UpdateRegisterResultAsync(
                    _objectMapper.Map<CreateHolderEto, CreateHolderResultGrainDto>(eventData));

            if (!result.Success)
            {
                _logger.LogError("update register grain fail, message:{message}", result.Message);
                throw new Exception(result.Message);
            }

            _logger.LogDebug("the third event: update register in es");
            var register = _objectMapper.Map<RegisterGrainDto, AccountRegisterIndex>(result.Data);

            register.RegisterStatus = GetAccountStatus(eventData.RegisterSuccess);
            await _registerRepository.UpdateAsync(register);

            await PublicRegisterMessageAsync(result.Data, eventData.Context);

            var duration = DateTime.UtcNow - register.CreateTime;
            _indicatorLogger.LogInformation(MonitorTag.Register, MonitorTag.Register.ToString(),
                (int)(duration?.TotalMilliseconds ?? 0));

            _logger.LogDebug("register update success: id: {id}, status: {status}", register.Id.ToString(),
                register.RegisterStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "update register info error, data: {data}", JsonConvert.SerializeObject(eventData));
        }
    }

    private async Task PublicRegisterMessageAsync(RegisterGrainDto register, HubRequestContext Context)
    {
        await _distributedEventBus.PublishAsync(new AccountRegisterCompletedEto
        {
            RegisterCompletedMessage = new RegisterCompletedMessageDto
            {
                RegisterStatus = GetAccountStatus(register.RegisterSuccess),
                CaAddress = register.CaAddress,
                CaHash = register.CaHash,
                RegisterMessage = register.RegisterMessage
            },
            Context = Context
        });
    }

    public async Task HandleEventAsync(SocialRecoveryEto eventData)
    {
        try
        {
            _logger.LogDebug("the second event: update recover grain.");

            var grain = _clusterClient.GetGrain<IRecoveryGrain>(eventData.GrainId);
            var updateResult = await grain.UpdateRecoveryResultAsync(
                _objectMapper.Map<SocialRecoveryEto, SocialRecoveryResultGrainDto>(eventData));

            if (!updateResult.Success)
            {
                _logger.LogError("{Message}", updateResult.Message);
            }

            _logger.LogDebug("the third event: update register in es");
            var recover = _objectMapper.Map<RecoveryGrainDto, AccountRecoverIndex>(updateResult.Data);
            recover.RecoveryStatus = GetAccountStatus(eventData.RecoverySuccess);
            await _recoverRepository.UpdateAsync(recover);

            await PublicRecoverMessageAsync(updateResult.Data, eventData.Context);

            var duration = DateTime.UtcNow - recover.CreateTime;
            _indicatorLogger.LogInformation(MonitorTag.SocialRecover, MonitorTag.SocialRecover.ToString(),
                (int)(duration?.TotalMilliseconds ?? 0));

            _logger.LogDebug("register update success: id: {id}, status: {status}", recover.Id.ToString(),
                recover.RecoveryStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }

    private async Task PublicRecoverMessageAsync(RecoveryGrainDto recover, HubRequestContext context)
    {
        await _distributedEventBus.PublishAsync(new AccountRecoverCompletedEto
        {
            RecoveryCompletedMessage = new RecoveryCompletedMessageDto
            {
                RecoveryStatus = GetAccountStatus(recover.RecoverySuccess),
                RecoveryMessage = recover.RecoveryMessage,
                CaHash = recover.CaHash,
                CaAddress = recover.CaAddress
            },
            Context = context
        });
    }

    private string GetAccountStatus(bool? accountSuccess) => !accountSuccess.HasValue
        ? AccountOperationStatus.Pending
        : accountSuccess.Value
            ? AccountOperationStatus.Pass
            : AccountOperationStatus.Fail;

    private async Task SwapGrainStateAsync(string caHash, string grainId)
    {
        var newDeviceGrain = _clusterClient.GetGrain<IDeviceGrain>(GrainIdHelper.GenerateGrainId("Device", caHash));
        var prevDeviceGrain = _clusterClient.GetGrain<IDeviceGrain>(GrainIdHelper.GenerateGrainId("Device", grainId));
        var salt = await prevDeviceGrain.GetOrGenerateSaltAsync();
        await newDeviceGrain.SetSaltAsync(salt);
    }

    private async Task AddGrowthInfoAsync(string caHash, string referralCode, string projectCode)
    {
    }
}
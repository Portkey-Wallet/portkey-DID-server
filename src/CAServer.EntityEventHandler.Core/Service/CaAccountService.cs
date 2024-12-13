using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Commons;
using CAServer.ContractEventHandler;
using CAServer.CryptoGift;
using CAServer.Dtos;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.Device;
using CAServer.Growth;
using CAServer.Hubs;
using CAServer.Monitor;
using CAServer.Monitor.Logger;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using Orleans;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core.Service;

public interface ICaAccountService
{
    Task HandleAccountRegisterCreateAsync(AccountRegisterCreateEto eventData);
    Task HandleAccountRecoverCreateAsync(AccountRecoverCreateEto eventData);
    Task HandleCreateHolderAsync(CreateHolderEto eventData);
    Task HandleSocialRecoveryAsync(SocialRecoveryEto eventData);
}

public class CaAccountService : ICaAccountService, ISingletonDependency
{
    private readonly INESTRepository<AccountRegisterIndex, Guid> _registerRepository;
    private readonly INESTRepository<AccountRecoverIndex, Guid> _recoverRepository;
    private readonly INESTRepository<AccelerateRegisterIndex, string> _accelerateRegisterRepository;
    private readonly INESTRepository<AccelerateRecoverIndex, string> _accelerateRecoverRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CaAccountService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IIndicatorLogger _indicatorLogger;
    private readonly IGrowthAppService _growthAppService;
    private readonly ICryptoGiftAppService _cryptoGiftAppService;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly IBusinessAlertProvider _businessAlertProvider;

    public CaAccountService(INESTRepository<AccountRegisterIndex, Guid> registerRepository,
        INESTRepository<AccountRecoverIndex, Guid> recoverRepository,
        IObjectMapper objectMapper,
        ILogger<CaAccountService> logger,
        IDistributedEventBus distributedEventBus,
        IClusterClient clusterClient,
        IIndicatorLogger indicatorLogger, IGrowthAppService growthAppService,
        INESTRepository<AccelerateRegisterIndex, string> accelerateRegisterRepository,
        INESTRepository<AccelerateRecoverIndex, string> accelerateRecoverRepository,
        ICryptoGiftAppService cryptoGiftAppService,
        IDistributedCache<string> distributedCache, IBusinessAlertProvider businessAlertProvider)
    {
        _registerRepository = registerRepository;
        _recoverRepository = recoverRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _indicatorLogger = indicatorLogger;
        _growthAppService = growthAppService;
        _accelerateRegisterRepository = accelerateRegisterRepository;
        _accelerateRecoverRepository = accelerateRecoverRepository;
        _cryptoGiftAppService = cryptoGiftAppService;
        _distributedCache = distributedCache;
        _businessAlertProvider = businessAlertProvider;
    }

    public async Task HandleAccountRegisterCreateAsync(AccountRegisterCreateEto eventData)
    {
        try
        {
            _logger.LogInformation("received account register message:{0}", JsonConvert.SerializeObject(eventData));
            _logger.LogInformation("the first event: create register");
            var register = _objectMapper.Map<AccountRegisterCreateEto, AccountRegisterIndex>(eventData);
            if (eventData.GuardianInfo.ZkLoginInfo != null)
            {
                register.GuardianInfo.ZkLoginInfo = eventData.GuardianInfo.ZkLoginInfo;
            }

            register.RegisterStatus = AccountOperationStatus.Pending;
            await _registerRepository.AddAsync(register);
            _logger.LogInformation("register add success: {data}", JsonConvert.SerializeObject(register));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[HandleMessageError] type:{type}, data:{data}, errMsg:{errMsg}",
                eventData.GetType().Name, JsonConvert.SerializeObject(eventData), e.StackTrace ?? "-");
        }
    }

    public async Task HandleAccountRecoverCreateAsync(AccountRecoverCreateEto eventData)
    {
        try
        {
            _logger.LogInformation("received account recover message:{0}", JsonConvert.SerializeObject(eventData));
            _logger.LogInformation("the first event: create recover");

            var recover = _objectMapper.Map<AccountRecoverCreateEto, AccountRecoverIndex>(eventData);

            recover.RecoveryStatus = AccountOperationStatus.Pending;
            await _recoverRepository.AddAsync(recover);
            _logger.LogInformation("recovery add success: {data}", JsonConvert.SerializeObject(recover));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[HandleMessageError] type:{type}, data:{data}, errMsg:{errMsg}",
                eventData.GetType().Name, JsonConvert.SerializeObject(eventData), e.StackTrace ?? "-");
        }
    }

    public async Task HandleCreateHolderAsync(CreateHolderEto eventData)
    {
        try
        {
            _logger.LogInformation("CreateHolderEto CryptoGiftTransferToRedPackage eventData:{0}",
                JsonConvert.SerializeObject(eventData));
            if (eventData.RegisterSuccess != null && eventData.RegisterSuccess.Value)
            {
                await _distributedCache.SetAsync(
                    string.Format(CryptoGiftConstant.RegisterCachePrefix, eventData.CaHash),
                    JsonConvert.SerializeObject(new CryptoGiftReferralDto
                    {
                        CaHash = eventData.CaHash,
                        CaAddress = eventData.CaAddress,
                        ReferralInfo = eventData.ReferralInfo,
                        IsNewUser = true,
                        IpAddress = eventData.IpAddress
                    }), new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1)
                    });
            }

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

            await RegisterAlertAsync(register);
            await PublicRegisterMessageAsync(result.Data, eventData.Context);

            var duration = DateTime.UtcNow - register.CreateTime;
            _indicatorLogger.LogInformation(MonitorTag.Register, MonitorTag.Register.ToString(),
                (int)(duration?.TotalMilliseconds ?? 0));

            _logger.LogInformation("register update success: id: {id}, status: {status}", register.Id.ToString(),
                register.RegisterStatus);

            await AddGrowthInfoAsync(eventData.CaHash, eventData.ReferralInfo);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[HandleMessageError] type:{type}, data:{data}, errMsg:{errMsg}",
                eventData.GetType().Name, JsonConvert.SerializeObject(eventData), e.StackTrace ?? "-");
        }
    }

    public async Task HandleSocialRecoveryAsync(SocialRecoveryEto eventData)
    {
        try
        {
            _logger.LogInformation("SocialRecoveryEto CryptoGiftTransferToRedPackage eventData:{0}",
                JsonConvert.SerializeObject(eventData));
            if (eventData.RecoverySuccess != null && eventData.RecoverySuccess.Value)
            {
                await _distributedCache.SetAsync(
                    string.Format(CryptoGiftConstant.SocialRecoveryCachePrefix, eventData.CaHash),
                    JsonConvert.SerializeObject(new CryptoGiftReferralDto
                    {
                        CaHash = eventData.CaHash,
                        CaAddress = eventData.CaAddress,
                        ReferralInfo = eventData.ReferralInfo,
                        IsNewUser = false,
                        IpAddress = eventData.IpAddress
                    }), new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1)
                    });
                await _distributedCache.RemoveAsync(string.Format(CryptoGiftConstant.RegisterCachePrefix,
                    eventData.CaHash));
                var cachedResult =
                    await _distributedCache.GetAsync(string.Format(CryptoGiftConstant.SocialRecoveryCachePrefix,
                        eventData.CaHash));
                _logger.LogInformation("SocialRecoveryEto CryptoGiftTransferToRedPackage cachedResult:{cachedResult}",
                    cachedResult);
            }

            _logger.LogDebug("the second event: update recover grain.");

            var grain = _clusterClient.GetGrain<IRecoveryGrain>(eventData.GrainId);
            var updateResult = await grain.UpdateRecoveryResultAsync(
                _objectMapper.Map<SocialRecoveryEto, SocialRecoveryResultGrainDto>(eventData));

            if (!updateResult.Success)
            {
                _logger.LogError("update recovery grain fail, {message}", updateResult.Message);
            }

            _logger.LogDebug("the third event: update recover in es");
            var recover = _objectMapper.Map<RecoveryGrainDto, AccountRecoverIndex>(updateResult.Data);
            recover.RecoveryStatus = GetAccountStatus(eventData.RecoverySuccess);
            await _recoverRepository.UpdateAsync(recover);

            await RecoverAlertAsync(recover);
            await PublicRecoverMessageAsync(updateResult.Data, eventData.Context);

            var duration = DateTime.UtcNow - recover.CreateTime;
            _indicatorLogger.LogInformation(MonitorTag.SocialRecover, MonitorTag.SocialRecover.ToString(),
                (int)(duration?.TotalMilliseconds ?? 0));

            _logger.LogDebug("recover update success: id: {id}, status: {status}", recover.Id.ToString(),
                recover.RecoveryStatus);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[HandleMessageError] type:{type}, data:{data}, errMsg:{errMsg}",
                eventData.GetType().Name, JsonConvert.SerializeObject(eventData), e.StackTrace ?? "-");
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

    private async Task AddGrowthInfoAsync(string caHash, ReferralInfo referralInfo)
    {
        if (referralInfo == null || referralInfo.ReferralCode.IsNullOrEmpty())
        {
            _logger.LogInformation("no need to add growth info, caHash:{caHash}", caHash);
            return;
        }

        await _growthAppService.CreateGrowthInfoAsync(caHash, referralInfo);
        _logger.LogInformation(
            "create growth info success, caHash:{caHash}, referralCode:{referralCode}, projectCode:{projectCode}",
            caHash, referralInfo.ReferralCode, referralInfo.ProjectCode ?? string.Empty);
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

        await _distributedEventBus.PublishAsync(new HolderExtraInfoCompletedEto
        {
            Status = GetAccountStatus(register.RegisterSuccess),
            CaAddress = register.CaAddress,
            CaHash = register.CaHash,
            GrainId = register.GrainId
        });
    }

    private async Task RegisterAlertAsync(AccountRegisterIndex registerIndex)
    {
        if (!registerIndex.RegisterSuccess == false) return;

        var title = "Register Error";
        _ = _businessAlertProvider.SendWebhookAsync(registerIndex.Id.ToString(), title,
            registerIndex.RegisterMessage ?? "-",
            JsonConvert.SerializeObject(registerIndex));
    }

    private async Task RecoverAlertAsync(AccountRecoverIndex recoverIndex)
    {
        if (!recoverIndex.RecoverySuccess == false) return;

        var title = "Register Error";
        _ = _businessAlertProvider.SendWebhookAsync(recoverIndex.Id.ToString(), title,
            recoverIndex.RecoveryMessage ?? "-",
            JsonConvert.SerializeObject(recoverIndex));
    }
}
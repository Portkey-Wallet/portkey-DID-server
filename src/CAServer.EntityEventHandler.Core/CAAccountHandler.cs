using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Account;
using CAServer.CAAccount;
using CAServer.CAAccount.Dtos;
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
using Orleans;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class CaAccountHandler : IDistributedEventHandler<AccountRegisterCreateEto>,
    IDistributedEventHandler<AccountRecoverCreateEto>,
    IDistributedEventHandler<CreateHolderEto>,
    IDistributedEventHandler<SocialRecoveryEto>,
    IDistributedEventHandler<AccelerateCreateHolderEto>,
    IDistributedEventHandler<AccelerateSocialRecoveryEto>,
    ITransientDependency
{
    private readonly ICaAccountService _caAccountService;
    private readonly INESTRepository<AccelerateRegisterIndex, string> _accelerateRegisterRepository;
    private readonly INESTRepository<AccelerateRecoverIndex, string> _accelerateRecoverRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CaAccountHandler> _logger;

    public CaAccountHandler(
        IObjectMapper objectMapper,
        ILogger<CaAccountHandler> logger,
        INESTRepository<AccelerateRegisterIndex, string> accelerateRegisterRepository,
        INESTRepository<AccelerateRecoverIndex, string> accelerateRecoverRepository,
        ICaAccountService caAccountService)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _accelerateRegisterRepository = accelerateRegisterRepository;
        _accelerateRecoverRepository = accelerateRecoverRepository;
        _caAccountService = caAccountService;
    }

    public async Task HandleEventAsync(AccountRegisterCreateEto eventData)
    {
        try
        {
            _ = _caAccountService.HandleAccountRegisterCreateAsync(eventData);
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
            _ = _caAccountService.HandleAccountRecoverCreateAsync(eventData);
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
            _ = _caAccountService.HandleCreateHolderAsync(eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "update register info error, data: {data}", JsonConvert.SerializeObject(eventData));
        }
    }


    public async Task HandleEventAsync(SocialRecoveryEto eventData)
    {
        try
        {
            _ = _caAccountService.HandleSocialRecoveryAsync(eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }

    private string GetAccountStatus(bool? accountSuccess) => !accountSuccess.HasValue
        ? AccountOperationStatus.Pending
        : accountSuccess.Value
            ? AccountOperationStatus.Pass
            : AccountOperationStatus.Fail;

    public async Task HandleEventAsync(AccelerateCreateHolderEto eventData)
    {
        var accelerateRegisterIndex = _objectMapper.Map<AccelerateCreateHolderEto, AccelerateRegisterIndex>(eventData);
        await _accelerateRegisterRepository.AddAsync(accelerateRegisterIndex);
    }

    public async Task HandleEventAsync(AccelerateSocialRecoveryEto eventData)
    {
        var accelerateRecoverIndex = _objectMapper.Map<AccelerateSocialRecoveryEto, AccelerateRecoverIndex>(eventData);
        await _accelerateRecoverRepository.AddAsync(accelerateRecoverIndex);
    }
}
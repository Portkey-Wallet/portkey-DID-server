using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Account;
using CAServer.Entities.Es;
using CAServer.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class CaAccountHandler : IDistributedEventHandler<AccountRegisterCreateEto>,
    IDistributedEventHandler<AccountRecoverCreateEto>,
    IDistributedEventHandler<AccountRegisterCompletedEto>,
    IDistributedEventHandler<AccountRecoverCompletedEto>,
    ITransientDependency
{
    private readonly INESTRepository<AccountRegisterIndex, Guid> _registerRepository;
    private readonly INESTRepository<AccountRecoverIndex, Guid> _recoverRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CaAccountHandler> _logger;

    public CaAccountHandler(INESTRepository<AccountRegisterIndex, Guid> registerRepository,
        INESTRepository<AccountRecoverIndex, Guid> recoverRepository,
        IObjectMapper objectMapper,
        ILogger<CaAccountHandler> logger)
    {
        _registerRepository = registerRepository;
        _recoverRepository = recoverRepository;
        _objectMapper = objectMapper;
        _logger = logger;
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
            _logger.LogError(ex,"{Message}", JsonConvert.SerializeObject(eventData));
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
            _logger.LogError(ex,"{Message}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(AccountRegisterCompletedEto eventData)
    {
        try
        {
            _logger.LogDebug("the third event: update register in es");
            var register = _objectMapper.Map<AccountRegisterCompletedEto, AccountRegisterIndex>(eventData);

            register.RegisterStatus = !eventData.RegisterSuccess.HasValue ? AccountOperationStatus.Pending :
                eventData.RegisterSuccess.Value ? AccountOperationStatus.Pass : AccountOperationStatus.Fail;
            
            await _registerRepository.UpdateAsync(register);
            _logger.LogDebug($"register update success: {JsonConvert.SerializeObject(eventData)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"{Message}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(AccountRecoverCompletedEto eventData)
    {
        try
        {
            _logger.LogDebug("the third event: update register in es");
            var recover = _objectMapper.Map<AccountRecoverCompletedEto, AccountRecoverIndex>(eventData);

            recover.RecoveryStatus = !eventData.RecoverySuccess.HasValue ? AccountOperationStatus.Pending :
                eventData.RecoverySuccess.Value ? AccountOperationStatus.Pass : AccountOperationStatus.Fail;

            await _recoverRepository.UpdateAsync(recover);
            _logger.LogDebug($"recovery update success: {JsonConvert.SerializeObject(eventData)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"{Message}", JsonConvert.SerializeObject(eventData));
        }
    }
}
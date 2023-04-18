using System;
using System.Threading.Tasks;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.ContractEventHandler;
using CAServer.Dtos;
using CAServer.Etos;
using CAServer.Grains.Grain.Account;
using CAServer.Hubs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EventHandler;

public class CAAccountHandler : IDistributedEventHandler<CreateHolderEto>,
    IDistributedEventHandler<SocialRecoveryEto>, ITransientDependency
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IClusterClient _clusterClient;
    private readonly IHubProvider _caHubProvider;
    private readonly ILogger<CAAccountHandler> _logger;
    private readonly IObjectMapper _objectMapper;

    public CAAccountHandler(IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        IHubProvider caHubProvider,
        ILogger<CAAccountHandler> logger,
        IObjectMapper objectMapper)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _caHubProvider = caHubProvider;
        _logger = logger;
        _objectMapper = objectMapper;
    }

    public async Task HandleEventAsync(CreateHolderEto eventData)
    {
        try
        {
            _logger.LogDebug("the second event: update register grain.");
            var grain = _clusterClient.GetGrain<IRegisterGrain>(eventData.GrainId);
            var result =
                await grain.UpdateRegisterResultAsync(
                    _objectMapper.Map<CreateHolderEto, CreateHolderResultGrainDto>(eventData));

            if (!result.Success)
            {
                _logger.LogError("{Message}", result.Message);
            }

            await _distributedEventBus.PublishAsync(
                _objectMapper.Map<RegisterGrainDto, AccountRegisterCompletedEto>(result.Data));

            _logger.LogDebug("send message to client.");
            await _caHubProvider.ResponseAsync(new HubResponse<RegisterCompletedMessageDto>
            {
                RequestId = eventData.Context.RequestId,
                Body = new RegisterCompletedMessageDto
                {
                    RegisterStatus = !eventData.RegisterSuccess.HasValue ? AccountOperationStatus.Pending :
                        eventData.RegisterSuccess.Value ? AccountOperationStatus.Pass : AccountOperationStatus.Fail,
                    RegisterMessage = eventData.RegisterMessage,
                    CaHash = eventData.CaHash,
                    CaAddress = eventData.CaAddress
                }
            }, eventData.Context.ClientId, method: "caAccountRegister");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
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

            await _distributedEventBus.PublishAsync(
                _objectMapper.Map<RecoveryGrainDto, AccountRecoverCompletedEto>(updateResult.Data));

            await _caHubProvider.ResponseAsync(new HubResponse<RecoveryCompletedMessageDto>
            {
                RequestId = eventData.Context.RequestId,
                Body = new RecoveryCompletedMessageDto
                {
                    RecoveryStatus = !eventData.RecoverySuccess.HasValue ? AccountOperationStatus.Pending :
                        eventData.RecoverySuccess.Value ? AccountOperationStatus.Pass : AccountOperationStatus.Fail,
                    RecoveryMessage = eventData.RecoveryMessage,
                    CaHash = eventData.CaHash,
                    CaAddress = eventData.CaAddress
                }
            }, eventData.Context.ClientId, method: "caAccountRecover");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }
}
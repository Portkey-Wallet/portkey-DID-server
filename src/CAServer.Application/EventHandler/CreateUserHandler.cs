using System;
using System.Threading.Tasks;
using CAServer.Etos;
using CAServer.Grains.Grain.Contacts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EventHandler;

public class CreateUserHandler : IDistributedEventHandler<CreateUserEto>, ITransientDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CreateUserHandler> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;

    public CreateUserHandler(
        IObjectMapper objectMapper,
        ILogger<CreateUserHandler> logger,
        IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
    }

    public async Task HandleEventAsync(CreateUserEto eventData)
    {
        try
        {
            var grain = _clusterClient.GetGrain<ICAHolderGrain>(eventData.UserId);
            var result = await grain.AddHolderAsync(_objectMapper.Map<CreateUserEto, CAHolderGrainDto>(eventData));

            if (result.Success)
            {
                await _distributedEventBus.PublishAsync(
                    _objectMapper.Map<CAHolderGrainDto, CreateCAHolderEto>(result.Data));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }
}
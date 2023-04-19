using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Guardian;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class GuardianHandler : IDistributedEventHandler<GuardianEto>, ITransientDependency
{
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CaAccountHandler> _logger;

    public GuardianHandler(
        INESTRepository<GuardianIndex, string> guardianRepository,
        IObjectMapper objectMapper,
        ILogger<CaAccountHandler> logger)
    {
        _guardianRepository = guardianRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(GuardianEto eventData)
    {
        try
        {
            var guardian = _objectMapper.Map<GuardianEto, GuardianIndex>(eventData);

            await _guardianRepository.AddAsync(guardian);

            _logger.LogDebug($"Guardian add success: {JsonConvert.SerializeObject(guardian)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}: {Data}", "Guardian add fail",
                JsonConvert.SerializeObject(eventData));
        }
    }
}
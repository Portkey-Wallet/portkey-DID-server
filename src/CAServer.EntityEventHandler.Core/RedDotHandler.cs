using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.RedDot.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class RedDotHandler : IDistributedEventHandler<RedDotEto>, ITransientDependency
{
    private readonly INESTRepository<RedDotIndex, string> _redDotRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<RedDotHandler> _logger;

    public RedDotHandler(INESTRepository<RedDotIndex, string> redDotRepository,
        IObjectMapper objectMapper,
        ILogger<RedDotHandler> logger)
    {
        _redDotRepository = redDotRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(RedDotEto eventData)
    {
        try
        {
            await _redDotRepository.AddAsync(_objectMapper.Map<RedDotEto, RedDotIndex>(eventData));
            _logger.LogInformation(
                "red dot info add or update success, redDotInfo:{redDotInfo}", JsonConvert.SerializeObject(eventData));
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "red dot info add or update error, redDotInfo:{redDotInfo}", JsonConvert.SerializeObject(eventData));
        }
    }
}
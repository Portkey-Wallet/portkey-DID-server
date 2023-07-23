using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Test.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class TestHandler : IDistributedEventHandler<TestEto>, ITransientDependency
{
    private readonly INESTRepository<TestIndex, string> _testRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TestHandler> _logger;

    public TestHandler(INESTRepository<TestIndex, string> testRepository,
        IObjectMapper objectMapper,
        ILogger<TestHandler> logger)
    {
        _testRepository = testRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(TestEto eventData)
    {
        try
        {
            await _testRepository.AddOrUpdateAsync(_objectMapper.Map<TestEto, TestIndex>(eventData));
            _logger.LogDebug("test add or update success: {EventData}", JsonConvert.SerializeObject(eventData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }
}
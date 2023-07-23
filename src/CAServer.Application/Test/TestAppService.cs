using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Test.Dtos;
using CAServer.Test.Etos;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.Test;

[RemoteService(false), DisableAuditing]
public class TestAppService : CAServerAppService, ITestAppService
{
    private readonly INESTRepository<TestIndex, string> _testRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TestAppService> _logger;
    private readonly IDistributedEventBus _eventBus;

    public TestAppService(INESTRepository<TestIndex, string> testRepository,
        IObjectMapper objectMapper,
        ILogger<TestAppService> logger,
        IDistributedEventBus eventBus)
    {
        _testRepository = testRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<TestResultDto> AddAsync(TestRequestDto input)
    {
        await _eventBus.PublishAsync(ObjectMapper.Map<TestRequestDto, TestEto>(input));
        return ObjectMapper.Map<TestRequestDto, TestResultDto>(input);
    }

    public async Task<TestResultDto> GetAsync(string id)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TestIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(id)));

        QueryContainer Filter(QueryContainerDescriptor<TestIndex> f) => f.Bool(b => b.Must(mustQuery));
        var test = await _testRepository.GetAsync(Filter);
        return ObjectMapper.Map<TestIndex, TestResultDto>(test);
    }
}
using AElf.Indexing.Elasticsearch;
using Volo.Abp.ObjectMapping;
using CAServer.Entities.Es;
using CAServer.Guardian;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Tests.Notify;
public class MockGuardianHandler:IDistributedEventHandler<GuardianEto>,ITransientDependency
{
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;

    private readonly IObjectMapper _objectMapper;

    public MockGuardianHandler(INESTRepository<GuardianIndex, string> guardianRepository,
        IObjectMapper objectMapper)
    {
        _guardianRepository = guardianRepository;
        _objectMapper = objectMapper;
    }
    
    public async Task HandleEventAsync(GuardianEto eventData)
    {
        var guardianindex = _objectMapper.Map<GuardianEto, GuardianIndex>(eventData);
        await _guardianRepository.AddOrUpdateAsync(guardianindex);
    }
}
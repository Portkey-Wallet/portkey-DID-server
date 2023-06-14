using AElf.Indexing.Elasticsearch;
using AElf.LinqToElasticSearch.Provider;
using Volo.Abp.ObjectMapping;
using CAServer.Entities.Es;
using CAServer.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Tests.CAHolder;
public class MockCaHolderHandler:IDistributedEventHandler<CreateUserEto>,ITransientDependency
{
    private readonly ILinqRepository<CAHolderIndex, Guid> _caHolderRepository;

    private readonly IObjectMapper _objectMapper;

    public MockCaHolderHandler(ILinqRepository<CAHolderIndex, Guid> caHolderRepository,
        IObjectMapper objectMapper)
    {
        _caHolderRepository = caHolderRepository;
        _objectMapper = objectMapper;
    }
    
    public async Task HandleEventAsync(CreateUserEto eventData)
    {
        var chainindex = _objectMapper.Map<CreateUserEto, CAHolderIndex>(eventData);
        await _caHolderRepository.AddOrUpdateAsync(chainindex);
    }
}
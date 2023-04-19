using AElf.Indexing.Elasticsearch;
using Volo.Abp.ObjectMapping;
using CAServer.Entities.Es;
using CAServer.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Tests.CAContact;
public class MockCaContactHandler:IDistributedEventHandler<ContactCreateEto>,ITransientDependency
{
    private readonly INESTRepository<ContactIndex, Guid> _contactRepository;

    private readonly IObjectMapper _objectMapper;

    public MockCaContactHandler(INESTRepository<ContactIndex, Guid> contactRepository,
        IObjectMapper objectMapper)
    {
        _contactRepository = contactRepository;
        _objectMapper = objectMapper;
    }
    
    public async Task HandleEventAsync(ContactCreateEto eventData)
    {
        var chainindex = _objectMapper.Map<ContactCreateEto, ContactIndex>(eventData);
        await _contactRepository.AddOrUpdateAsync(chainindex);
    }
}
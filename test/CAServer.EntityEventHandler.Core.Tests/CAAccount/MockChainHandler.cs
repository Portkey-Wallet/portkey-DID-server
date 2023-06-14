using AElf.Indexing.Elasticsearch;
using AElf.LinqToElasticSearch.Provider;
using Volo.Abp.ObjectMapping;
using CAServer.Entities.Es;
using CAServer.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Tests.CAAccount;
public class MockCaAccountHandler:IDistributedEventHandler<AccountRegisterCreateEto>,ITransientDependency
{
    private readonly ILinqRepository<AccountRegisterIndex, Guid> _chainRepository;

    private readonly IObjectMapper _objectMapper;

    public MockCaAccountHandler(ILinqRepository<AccountRegisterIndex, Guid> chainRepository,
        IObjectMapper objectMapper)
    {
        _chainRepository = chainRepository;
        _objectMapper = objectMapper;
    }
    
    public async Task HandleEventAsync(AccountRegisterCreateEto eventData)
    {
        var accountindex = _objectMapper.Map<AccountRegisterCreateEto, AccountRegisterIndex>(eventData);
        await _chainRepository.AddOrUpdateAsync(accountindex);
    }
}
using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;

namespace CAServer.EntityEventHandler.Core;

public class CAContactHandler : IDistributedEventHandler<ContactCreateEto>,
    IDistributedEventHandler<ContactUpdateEto>,
    ITransientDependency
{
    private readonly INESTRepository<ContactIndex, Guid> _contactRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CAContactHandler> _logger;

    public CAContactHandler(INESTRepository<ContactIndex, Guid> contactRepository,
        IObjectMapper objectMapper,
        ILogger<CAContactHandler> logger)
    {
        _contactRepository = contactRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(ContactCreateEto eventData)
    {
        try
        {
            var contact = _objectMapper.Map<ContactCreateEto, ContactIndex>(eventData);
            
            await _contactRepository.AddAsync(contact);
            _logger.LogDebug("add success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(ContactUpdateEto eventData)
    {
        try
        {
            var contact = _objectMapper.Map<ContactUpdateEto, ContactIndex>(eventData);

            await _contactRepository.UpdateAsync(contact);
            _logger.LogDebug("update success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }
}
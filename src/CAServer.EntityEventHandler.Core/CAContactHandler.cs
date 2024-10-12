using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Monitor.Interceptor;
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

    [ExceptionHandler(typeof(Exception),
        Message = "ContactCreateEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(ContactCreateEto eventData)
    {
        var contact = _objectMapper.Map<ContactCreateEto, ContactIndex>(eventData);
            
        await _contactRepository.AddAsync(contact);
        _logger.LogDebug("ContactUpdateEto add success");
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ContactUpdateEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(ContactUpdateEto eventData)
    {
        var contact = _objectMapper.Map<ContactUpdateEto, ContactIndex>(eventData);

        await _contactRepository.UpdateAsync(contact);
        _logger.LogDebug("ContactUpdateEto update success");
    }
}
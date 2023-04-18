using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class CAHolderHandler : IDistributedEventHandler<CreateCAHolderEto>,
    IDistributedEventHandler<UpdateCAHolderEto>
    , ITransientDependency
{
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CAHolderHandler> _logger;

    public CAHolderHandler(INESTRepository<CAHolderIndex, Guid> caHolderRepository,
        IObjectMapper objectMapper,
        ILogger<CAHolderHandler> logger)
    {
        _caHolderRepository = caHolderRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(CreateCAHolderEto eventData)
    {
        try
        {
            await _caHolderRepository.AddAsync(_objectMapper.Map<CreateCAHolderEto, CAHolderIndex>(eventData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(UpdateCAHolderEto eventData)
    {
        try
        {
            await _caHolderRepository.UpdateAsync(_objectMapper.Map<UpdateCAHolderEto, CAHolderIndex>(eventData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }
}
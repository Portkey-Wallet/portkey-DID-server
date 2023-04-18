using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Etos.Chain;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class ChainHandler : IDistributedEventHandler<ChainCreateEto>,
    IDistributedEventHandler<ChainUpdateEto>,
    IDistributedEventHandler<ChainDeleteEto>,
    ITransientDependency
{
    private readonly INESTRepository<ChainsInfoIndex, string> _chainsInfoRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ChainHandler> _logger;

    public ChainHandler(INESTRepository<ChainsInfoIndex, string> chainsInfoRepository,
        IObjectMapper objectMapper,
        ILogger<ChainHandler> logger)
    {
        _chainsInfoRepository = chainsInfoRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(ChainCreateEto eventData)
    {
        try
        {
            await _chainsInfoRepository.AddAsync(_objectMapper.Map<ChainCreateEto, ChainsInfoIndex>(eventData));
            _logger.LogDebug($"Chain info add success: {JsonConvert.SerializeObject(eventData)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"{Message}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(ChainUpdateEto eventData)
    {
        try
        {
            await _chainsInfoRepository.UpdateAsync(_objectMapper.Map<ChainUpdateEto, ChainsInfoIndex>(eventData));
            _logger.LogDebug($"chain info update success: {JsonConvert.SerializeObject(eventData)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"{Message}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(ChainDeleteEto eventData)
    {
        try
        {
            await _chainsInfoRepository.DeleteAsync(_objectMapper.Map<ChainDeleteEto, ChainsInfoIndex>(eventData));
            _logger.LogDebug("chain info delete success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"{Message}", JsonConvert.SerializeObject(eventData));
        }
    }
}
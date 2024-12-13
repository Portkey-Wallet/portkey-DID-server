using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Guardian;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core.Service;

public interface IGuardianService
{
    Task AddGuardianAsync(GuardianEto eventData);
    Task DeleteGuardianAsync(GuardianDeleteEto eventData);
}

public class GuardianService : IGuardianService, ISingletonDependency
{
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<GuardianService> _logger;

    public GuardianService(
        INESTRepository<GuardianIndex, string> guardianRepository,
        IObjectMapper objectMapper,
        ILogger<GuardianService> logger)
    {
        _guardianRepository = guardianRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task AddGuardianAsync(GuardianEto eventData)
    {
        try
        {
            var guardian = _objectMapper.Map<GuardianEto, GuardianIndex>(eventData);
            await _guardianRepository.AddOrUpdateAsync(guardian);
            _logger.LogDebug("Guardian add or update success, id: {id}", eventData.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Guardian add error, {data}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task DeleteGuardianAsync(GuardianDeleteEto eventData)
    {
        try
        {
            var guardian = _objectMapper.Map<GuardianDeleteEto, GuardianIndex>(eventData);
            await _guardianRepository.UpdateAsync(guardian);

            _logger.LogDebug("Guardian delete success, id: {id}", eventData.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Guardian delete error, id: {id}", eventData.Id);
        }
    }
}
using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Upgrade.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class UpgradeHandler : IDistributedEventHandler<CreateUpgradeInfoEto>, ITransientDependency
{
    private readonly INESTRepository<UpgradeInfoIndex, string> _upgradeInfoRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<NotifyHandler> _logger;

    public UpgradeHandler(INESTRepository<UpgradeInfoIndex, string> upgradeInfoRepository, IObjectMapper objectMapper,
        ILogger<NotifyHandler> logger)
    {
        _upgradeInfoRepository = upgradeInfoRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(CreateUpgradeInfoEto eventData)
    {
        try
        {
            await _upgradeInfoRepository.AddOrUpdateAsync(
                _objectMapper.Map<CreateUpgradeInfoEto, UpgradeInfoIndex>(eventData));
            _logger.LogInformation("UpgradeInfo add or update success, id:{id}, version:{version}, isPopup:{isPopup}",
                eventData.Id, eventData.Version, eventData.IsPopup);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UpgradeInfo add or update error, id:{id}, version:{version}, isPopup:{isPopup}",
                eventData.Id, eventData.Version, eventData.IsPopup);
        }
    }
}
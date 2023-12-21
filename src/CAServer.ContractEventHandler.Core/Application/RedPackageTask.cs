using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.RedPackage;
using CAServer.RedPackage.Etos;
using Hangfire;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class RedPackageTask
{
    private readonly INESTRepository<RedPackageIndex, Guid> _redPackageRepository;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<RedPackageTask> _logger;
    private readonly IDistributedEventBus _distributedEventBus;


    public RedPackageTask(INESTRepository<RedPackageIndex, Guid> redPackageRepository, IClusterClient clusterClient,
        ILogger<RedPackageTask> logger, IDistributedEventBus distributedEventBus)
    {
        _redPackageRepository = redPackageRepository;
        _clusterClient = clusterClient;
        _logger = logger;
        _distributedEventBus = distributedEventBus;
    }

    // [Queue("redpackage")]
    public async Task ExpireRedPackageRedPackageAsync(Guid redPackageId)
    {
        var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageId);

        await grain.ExpireRedPackage();
        _logger.LogInformation("Expire RedPackage id:{id}", redPackageId);
         await _distributedEventBus.PublishAsync(new RefundRedPackageEto()
        {
            RedPackageId = redPackageId
        });
    }
    
    
}
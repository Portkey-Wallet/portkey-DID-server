using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.RedPackage;
using Hangfire;
using Microsoft.Extensions.Logging;
using Orleans;

namespace CAServer.EntityEventHandler.Core;

public class RedPackageTask
{
    private readonly INESTRepository<RedPackageIndex, Guid> _redPackageRepository;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<RedPackageTask> _logger;

    public RedPackageTask(INESTRepository<RedPackageIndex, Guid> redPackageRepository, IClusterClient clusterClient,
        ILogger<RedPackageTask> logger)
    {
        _redPackageRepository = redPackageRepository;
        _clusterClient = clusterClient;
        _logger = logger;
    }

    [Queue("redpackage")]
    public async Task ExpireRedPackageRedPackageAsync(Guid redPackageId)
    {
        _logger.LogInformation("Expire RedPackage id:{id}", redPackageId);

        var grain = _clusterClient.GetGrain<IRedPackageGrain>(redPackageId);

        await grain.ExpireRedPackage();
        _logger.LogInformation("Expire RedPackage id:{id}", redPackageId);
    }
}
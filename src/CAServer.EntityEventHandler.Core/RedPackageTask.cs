using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.RedPackage;
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

    public async Task DeleteRedPackageAsync(Guid redPackageId)
    {
        var grain = _clusterClient.GetGrain<IRedPackageGrain>(redPackageId);

        await grain.DeleteRedPackage();
        _logger.LogInformation("delete RedPackage id:{id}", redPackageId);
    }
}
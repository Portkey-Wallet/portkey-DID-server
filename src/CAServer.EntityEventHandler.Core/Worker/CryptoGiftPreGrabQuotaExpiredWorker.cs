using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.CryptoGift;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.Grains.Grain.CryptoGift;
using CAServer.Grains.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class CryptoGiftPreGrabQuotaExpiredWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly INESTRepository<RedPackageIndex, Guid> _redPackageIndexRepository;
    private readonly IClusterClient _clusterClient;
    private readonly ICryptoGiftProvider _cryptoGiftProvider;
    private readonly ILogger<CryptoGiftPreGrabQuotaExpiredWorker> _logger;
    
    public CryptoGiftPreGrabQuotaExpiredWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        INESTRepository<RedPackageIndex, Guid> redPackageIndexRepository,
        ICryptoGiftProvider cryptoGiftProvider,
        ILogger<CryptoGiftPreGrabQuotaExpiredWorker> logger) : base(timer, serviceScopeFactory)
    {
        _redPackageIndexRepository = redPackageIndexRepository;
        _cryptoGiftProvider = cryptoGiftProvider;
        _logger = logger;
        Timer.Period = WorkerConst.CryptoGiftExpiredTimePeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogInformation("CryptoGiftPreGrabQuotaExpiredWorker is beginning");
        var mustQuery = new List<Func<QueryContainerDescriptor<RedPackageIndex>, QueryContainer>>();
        mustQuery.Add(q => 
            q.Term(i => i.Field(f => f.RedPackageDisplayType).Value((int)RedPackageDisplayType.CryptoGift)));
        mustQuery.Add(q => 
            q.Term(i => i.Field(f => f.CreateTime > DateTimeOffset.Now.Subtract(TimeSpan.FromDays(1)).ToUnixTimeMilliseconds())));
        QueryContainer Filter(QueryContainerDescriptor<RedPackageIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, cryptoGiftIndices) = await _redPackageIndexRepository.GetListAsync(Filter);
        _logger.LogInformation("CryptoGiftPreGrabQuotaExpiredWorker cryptoGiftIndices:{0}", JsonConvert.SerializeObject(cryptoGiftIndices));
        if (cryptoGiftIndices.IsNullOrEmpty())
        {
            return;
        }

        var expiredTimeLimitMillis = _cryptoGiftProvider.GetExpirationSeconds() * 1000;
        foreach (var cryptoGiftIndex in cryptoGiftIndices)
        {
            var grain = _clusterClient.GetGrain<ICryptoGiftGran>(cryptoGiftIndex.RedPackageId);
            var ctrCryptoGiftResult = await grain.GetCryptoGift(cryptoGiftIndex.RedPackageId);
            if (!ctrCryptoGiftResult.Success || ctrCryptoGiftResult.Data == null)
            {
                continue;
            }

            bool modified = false;
            var cryptoGiftDto = ctrCryptoGiftResult.Data;
            foreach (var preGrabItem in cryptoGiftDto.Items.Where(preGrabItem => PreGrabItemCondition(preGrabItem, expiredTimeLimitMillis)))
            {
                modified = true;
                preGrabItem.GrabbedStatus = GrabbedStatus.Expired;
            }
            if (modified)
            {
                await grain.UpdateCryptoGift(cryptoGiftDto);
            }
        }
    }

    private bool PreGrabItemCondition(PreGrabItem preGrabItem, long expiredTimeLimitMillis)
    {
        return GrabbedStatus.Created.Equals(preGrabItem.GrabbedStatus)
               && preGrabItem.GrabTime >= DateTimeOffset.Now.ToUnixTimeMilliseconds() + expiredTimeLimitMillis;
    }
}
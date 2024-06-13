using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.EnumType;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class CryptoGiftPreGrabQuotaExpiredWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly INESTRepository<RedPackageIndex, Guid> _redPackageIndexRepository;
    private readonly ILogger<CryptoGiftPreGrabQuotaExpiredWorker> _logger;
    
    public CryptoGiftPreGrabQuotaExpiredWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        INESTRepository<RedPackageIndex, Guid> redPackageIndexRepository,
        ILogger<CryptoGiftPreGrabQuotaExpiredWorker> logger) : base(timer, serviceScopeFactory)
    {
        _redPackageIndexRepository = redPackageIndexRepository;
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
        
    }
}
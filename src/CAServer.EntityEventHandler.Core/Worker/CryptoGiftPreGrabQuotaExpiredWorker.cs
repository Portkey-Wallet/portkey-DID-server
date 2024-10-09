using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.CryptoGift;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.Grains.Grain.CryptoGift;
using CAServer.Grains.Grain.RedPackage;
using CAServer.Grains.State;
using CAServer.RedPackage.Dtos;
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
    private const long ExtraDeviationMilliSeconds = 90000;
    private readonly INESTRepository<RedPackageIndex, Guid> _redPackageIndexRepository;
    private readonly IClusterClient _clusterClient;
    private readonly ICryptoGiftProvider _cryptoGiftProvider;
    private readonly ILogger<CryptoGiftPreGrabQuotaExpiredWorker> _logger;
    
    public CryptoGiftPreGrabQuotaExpiredWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        INESTRepository<RedPackageIndex, Guid> redPackageIndexRepository,
        ICryptoGiftProvider cryptoGiftProvider,
        IClusterClient clusterClient,
        ILogger<CryptoGiftPreGrabQuotaExpiredWorker> logger) : base(timer, serviceScopeFactory)
    {
        _redPackageIndexRepository = redPackageIndexRepository;
        _cryptoGiftProvider = cryptoGiftProvider;
        _clusterClient = clusterClient;
        _logger = logger;
        Timer.Period = WorkerConst.CryptoGiftExpiredTimePeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var yesterday = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(1)).ToUnixTimeMilliseconds();
        var mustQuery = new List<Func<QueryContainerDescriptor<RedPackageIndex>, QueryContainer>>();
        mustQuery.Add(q => 
            q.Term(i => i.Field(f => f.RedPackageDisplayType).Value((int)RedPackageDisplayType.CryptoGift)));
        // mustQuery.Add(q => 
        //     q.Term(i => i.Field(f => f.CreateTime > yesterday)));
        QueryContainer Filter(QueryContainerDescriptor<RedPackageIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, cryptoGiftIndices) = await _redPackageIndexRepository.GetListAsync(Filter);
        if (cryptoGiftIndices.IsNullOrEmpty())
        {
            return;
        }

        cryptoGiftIndices = cryptoGiftIndices.Where(crypto => crypto.CreateTime > yesterday).ToList();
        if (cryptoGiftIndices.IsNullOrEmpty())
        {
            return;
        }
        var expiredTimeLimitMillis = _cryptoGiftProvider.GetExpirationSeconds() * 1000;
        foreach (var cryptoGiftIndex in cryptoGiftIndices)
        {
            try
            {
                var redPackageGrain = _clusterClient.GetGrain<ICryptoBoxGrain>(cryptoGiftIndex.RedPackageId);
                var resultDto = await redPackageGrain.GetRedPackage(cryptoGiftIndex.RedPackageId);
                if (!resultDto.Success || resultDto.Data == null)
                {
                    continue;
                }
                var grain = _clusterClient.GetGrain<ICryptoGiftGran>(cryptoGiftIndex.RedPackageId);
                var ctrCryptoGiftResult = await grain.GetCryptoGift(cryptoGiftIndex.RedPackageId);
                if (!ctrCryptoGiftResult.Success || ctrCryptoGiftResult.Data == null)
                {
                    continue;
                }
        
                var cryptoGiftDto = ctrCryptoGiftResult.Data;
                var redPackageDetailDto = resultDto.Data;
                var expiredPreGrabItems = cryptoGiftDto.Items.Where(preGrabItem => PreGrabItemCondition(preGrabItem, expiredTimeLimitMillis)).ToList();
                if (expiredPreGrabItems.IsNullOrEmpty())
                {
                    continue;
                }
                var needReturnQuota = GetNeedReturnQuotaStatus(redPackageDetailDto);
                foreach (var preGrabItem in expiredPreGrabItems.ToList())
                {
                    preGrabItem.GrabbedStatus = GrabbedStatus.Expired;
                    if (needReturnQuota)
                    {
                        PreGrabBucketItemDto preGrabBucketItemDto = cryptoGiftDto.BucketClaimed.FirstOrDefault(bucket => bucket.Index.Equals(preGrabItem.Index));
                        if (preGrabBucketItemDto == null)
                        {
                            continue;
                        }
                        cryptoGiftDto.PreGrabbedAmount -= preGrabBucketItemDto.Amount;
                        cryptoGiftDto.BucketClaimed.Remove(preGrabBucketItemDto);
                        preGrabBucketItemDto.IdentityCode = string.Empty;
                        cryptoGiftDto.BucketNotClaimed.Add(preGrabBucketItemDto);
                    }
                }

                var updateCryptoGift = await grain.UpdateCryptoGift(cryptoGiftDto);
                if (updateCryptoGift.Success && updateCryptoGift.Data != null)
                {
                    _logger.LogInformation("CryptoGiftPreGrabQuotaExpiredWorker updateCryptoGift result:{0}", JsonConvert.SerializeObject(updateCryptoGift));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "worker redpackageId:{0}", cryptoGiftIndex.RedPackageId);
            }
        }
    }

    private bool GetNeedReturnQuotaStatus(RedPackageDetailDto redPackageDetailDto)
    {
        return !(RedPackageStatus.FullyClaimed.Equals(redPackageDetailDto.Status)
               || RedPackageStatus.Cancelled.Equals(redPackageDetailDto.Status)
               || RedPackageStatus.Expired.Equals(redPackageDetailDto.Status));
    }

    private bool PreGrabItemCondition(PreGrabItem preGrabItem, long expiredTimeLimitMillis)
    {
        return (GrabbedStatus.Created.Equals(preGrabItem.GrabbedStatus)
                   && (preGrabItem.GrabTime + expiredTimeLimitMillis - ExtraDeviationMilliSeconds) <= DateTimeOffset.Now.ToUnixTimeMilliseconds());
    }
}
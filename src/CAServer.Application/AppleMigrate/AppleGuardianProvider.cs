using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.Entities.Es;
using Nest;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;

namespace CAServer.AppleMigrate;

[RemoteService(false), DisableAuditing]
public class AppleGuardianProvider : CAServerAppService, IAppleGuardianProvider
{
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;
    private readonly IDistributedCache<AppleUserTransfer> _distributedCache;

    public AppleGuardianProvider(
        INESTRepository<GuardianIndex, string> guardianRepository,
        IDistributedCache<AppleUserTransfer> distributedCache)
    {
        _guardianRepository = guardianRepository;
        _distributedCache = distributedCache;
    }

    public async Task<int> SetAppleGuardianIntoCache()
    {
        // var count = 0;
        // var userTransfer = await _distributedCache.GetAsync(CommonConstant.AppleUserTransferKey);
        // if (userTransfer?.AppleUserTransferInfos is { Count: > 0 })
        // {
        //     throw new UserFriendlyException("all user info already in cache.");
        // }
        //
        // var guardians = new List<GuardianIndex>();
        // var appleUserTransferInfos = new List<AppleUserTransferInfo>();
        //
        // var skip = 0;
        // var limit = 100;
        //
        // var list = await GetGuardiansAsync(skip, limit);
        // guardians.AddRange(list.Where(t=>t.Identifier));
        //
        // foreach (var guardianIndex in guardians)
        // {
        //     appleUserTransferInfos.Add(new AppleUserTransferInfo()
        //     {
        //         UserId = guardianIndex.Identifier
        //     });
        // }
        //
        // var userTransfer = await _distributedCache.GetAsync(CommonConstant.AppleUserTransferKey);
        // if (userTransfer?.AppleUserTransferInfos is { Count: > 0 })
        // {
        //     throw new UserFriendlyException("all user info already in cache.");
        // }
        //
        // await _distributedCache.SetAsync(new AppleUserTransfer());

        throw new System.NotImplementedException();
    }

    public async Task<List<GuardianIndex>> GetGuardiansAsync(int skip, int limit)
    {
        var esResult = await _guardianRepository.GetListAsync(skip: skip, limit: limit);

        if (esResult.Item1 <= 0)
        {
            throw new UserFriendlyException("get guardians from es fail.");
        }

        var guardians = esResult.Item2;

        return guardians;
    }
}
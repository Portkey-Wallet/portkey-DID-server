using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.AppleMigrate.Dtos;
using CAServer.Commons;
using CAServer.Entities.Es;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
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
    private readonly INESTRepository<UserExtraInfoIndex, string> _userExtraInfoRepository;
    private readonly IDistributedCache<AppleMigrateResponseDto> _migrateUserInfo;
    private static long _guardianTotalCount = 0;

    public AppleGuardianProvider(
        INESTRepository<GuardianIndex, string> guardianRepository,
        IDistributedCache<AppleUserTransfer> distributedCache,
        INESTRepository<UserExtraInfoIndex, string> userExtraInfoRepository,
        IDistributedCache<AppleMigrateResponseDto> migrateUserInfo)
    {
        _guardianRepository = guardianRepository;
        _distributedCache = distributedCache;
        _userExtraInfoRepository = userExtraInfoRepository;
        _migrateUserInfo = migrateUserInfo;
    }

    public async Task<int> SetAppleGuardianIntoCache()
    {
        var count = 0;
        var userTransfer = await _distributedCache.GetAsync(CommonConstant.AppleUserTransferKey);
        if (userTransfer?.AppleUserTransferInfos is { Count: > 0 })
        {
            throw new UserFriendlyException("all user info already in cache.");
        }

        var guardians = new List<GuardianIndex>();
        var appleUserTransferInfos = new List<AppleUserTransferInfo>();

        var skip = 0;
        var limit = 100;

        var list = await GetGuardiansAsync(skip, limit);
        guardians.AddRange(list.Where(t => AppleHelper.IsAppleUserId(t.Identifier)));
        var queryCount = (int)(_guardianTotalCount / limit) + 1;

        Logger.LogInformation("will query guardian index {count} times.", queryCount);
        for (var i = 1; i < queryCount; i++)
        {
            skip = i * limit;
            var cur = await GetGuardiansAsync(skip, limit);
            guardians.AddRange(cur.Where(t => AppleHelper.IsAppleUserId(t.Identifier)));
        }

        foreach (var guardianIndex in guardians)
        {
            if (guardianIndex == null || guardianIndex.Identifier.IsNullOrWhiteSpace()) continue;

            appleUserTransferInfos.Add(new AppleUserTransferInfo()
            {
                UserId = guardianIndex.Identifier
            });

            count++;
        }

        Logger.LogInformation("apple user guardian count: {count}.", guardians.Count);

        var ids = appleUserTransferInfos.Select(t => t.UserId).ToList();

        var userExtraInfos = await GetUserExtraInfoAsync();
        foreach (var info in userExtraInfos)
        {
            if (info == null || info.Id.IsNullOrWhiteSpace()) continue;

            var id = info.Id.Replace("UserExtraInfo-", "").Trim();
            if (!ids.Contains(id))
            {
                appleUserTransferInfos.Add(new AppleUserTransferInfo()
                {
                    UserId = id
                });

                Logger.LogWarning("why user id just in extra info index? {userId}", id);
                count++;
            }
        }

        Logger.LogInformation("apple user guardian and user extra info count: {count}.", guardians.Count);

        var userTransferEnd = await _distributedCache.GetAsync(CommonConstant.AppleUserTransferKey);
        if (userTransferEnd?.AppleUserTransferInfos is { Count: > 0 })
        {
            throw new UserFriendlyException("all user info already in cache.");
        }

        await _distributedCache.SetAsync(CommonConstant.AppleUserTransferKey, new AppleUserTransfer()
        {
            AppleUserTransferInfos = appleUserTransferInfos
        }, new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTime.UtcNow.AddYears(10)
        });

        return count;
    }

    public async Task<AppleUserTransfer> GetAppleGuardianIntoCache()
    {
        return await _distributedCache.GetAsync(CommonConstant.AppleUserTransferKey);
    }

    public async Task<List<GuardianIndex>> GetGuardiansAsync(int skip, int limit)
    {
        var esResult = await _guardianRepository.GetListAsync(skip: skip, limit: limit);

        if (esResult.Item1 <= 0)
        {
            throw new UserFriendlyException("get guardians from es fail.");
        }

        if (_guardianTotalCount <= 0)
        {
            _guardianTotalCount = esResult.Item1;
        }

        var guardians = esResult.Item2;

        return guardians ?? new List<GuardianIndex>();
    }

    public async Task<List<UserExtraInfoIndex>> GetUserExtraInfoAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserExtraInfoIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.GuardianType).Value("Apple")));

        QueryContainer Filter(QueryContainerDescriptor<UserExtraInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        var esResult = await _userExtraInfoRepository.GetListAsync(Filter);

        if (esResult.Item1 <= 0)
        {
            throw new UserFriendlyException("get user extra info from es fail.");
        }

        Logger.LogInformation("user extra info count:{count}", esResult.Item1);
        var users = esResult.Item2;
        return users ?? new List<UserExtraInfoIndex>();
    }

    public async Task<object> GetMigrateResult(string userId)
    {
        var userInfo = await GetTransferInfoFromCache(userId);
        if (userInfo == null)
        {
            throw new UserFriendlyException($"user not in cache, userId:{userId}");
        }

        var migrateResponseDto = await _migrateUserInfo.GetAsync(CommonConstant.AppleMigrateUserKey + userId);
        if (migrateResponseDto == null)
        {
            throw new UserFriendlyException($"migrate guardian info not in cache, userId:{userId}");
        }

        var guardians = await GetGuardianAsync(userInfo.Sub);
        if (guardians == null)
        {
            throw new UserFriendlyException($"migrate guardian info not in es, userId:{userId}");
        }

        return guardians;
    }

    private async Task<AppleUserTransferInfo> GetTransferInfoFromCache(string userId)
    {
        if (userId.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("userId is must");
        }

        var userTransfer = await _distributedCache.GetAsync(CommonConstant.AppleUserTransferKey);
        if (userTransfer?.AppleUserTransferInfos == null || userTransfer?.AppleUserTransferInfos.Count <= 0)
        {
            throw new UserFriendlyException("in SetTransferSubAsync,  all user info not in cache.");
        }

        return userTransfer.AppleUserTransferInfos.FirstOrDefault(t => t.UserId == userId);
    }

    private async Task<GuardianIndex> GetGuardianAsync(string userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Identifier).Value(userId)));

        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) => f.Bool(b => b.Must(mustQuery));
        var esResult = await _guardianRepository.GetAsync(Filter);

        return esResult;
    }
}
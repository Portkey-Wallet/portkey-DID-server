using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.CAActivity.Provider;
using CAServer.Cache;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Growth.Dtos;
using CAServer.Growth.Provider;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization;
using Volo.Abp.Users;

namespace CAServer.Growth;

[RemoteService(false), DisableAuditing]
public class GrowthStatisticAppService : CAServerAppService, IGrowthStatisticAppService
{
    private readonly IGrowthProvider _growthProvider;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly ICacheProvider _cacheProvider;
    private readonly IActivityProvider _activityProvider;
    private readonly ILogger<GrowthStatisticAppService> _logger;
    private readonly IUserAssetsProvider _userAssetsProvider;
    

    public GrowthStatisticAppService(IGrowthProvider growthProvider,
        INESTRepository<CAHolderIndex, Guid> caHolderRepository,
        ICacheProvider cacheProvider,
        IActivityProvider activityProvider, ILogger<GrowthStatisticAppService> logger, IUserAssetsProvider userAssetsProvider)
    {
        _growthProvider = growthProvider;
        _caHolderRepository = caHolderRepository;
        _cacheProvider = cacheProvider;
        _activityProvider = activityProvider;
        _logger = logger;
        _userAssetsProvider = userAssetsProvider;
    }

    public async Task<ReferralResponseDto> GetReferralInfoAsync(ReferralRequestDto input)
    {
        var result = await GetReferralInfoTreeAsync(input);

        if (input.SearchOrigin)
        {
            await SetOriginAsync(result);
        }

        return result;
    }

    public async Task<int> GetReferralTotalCountAsync(ReferralRecordRequestDto input)
    {
        // if (!CurrentUser.Id.HasValue)
        // {
        //     throw new AbpAuthorizationException("Unauthorized.");
        // }
        //
        // var caHolder = await _userAssetsProvider.GetCaHolderIndexAsync(CurrentUser.GetId());
        // var growthInfo = await _growthProvider.GetGrowthInfoByCaHashAsync(caHolder.CaHash);
        var growthInfo = await _growthProvider.GetGrowthInfoByCaHashAsync(input.CaHash);
        if (growthInfo == null)
        {
            return 0;
        }

        // Get First invite users
        var indexerReferralInfo =
            await _growthProvider.GetReferralInfoAsync(new List<string>(), new List<string> { growthInfo.InviteCode },
                new List<string> { MethodName.CreateCAHolder }, 0, 0);
        return indexerReferralInfo.ReferralInfo.Count;
    }

    public async Task<ReferralRecordResponseDto> GetReferralRecordList(ReferralRecordRequestDto input)
    {
        // if (!CurrentUser.Id.HasValue)
        // {
        //     throw new AbpAuthorizationException("Unauthorized.");
        // }
        // var caHolder = await _userAssetsProvider.GetCaHolderIndexAsync(CurrentUser.GetId());
        
        var hasNextPage = true;
        var referralRecordList =
            await _growthProvider.GetReferralRecordListAsync(null, input.CaHash, input.Skip, input.Limit);
        if (referralRecordList.Count < input.Limit)
        {
            hasNextPage = false;
        }
        
        var caHashes = referralRecordList.Select(t => t.CaHash).Distinct().ToList();
        foreach (var index in referralRecordList)
        {
            _logger.LogDebug("Referral caHash is {hash}",JsonConvert.SerializeObject(index));
            var holder = await _userAssetsProvider.GetCaHolderIndexByCahashAsync(index.CaHash);
            _logger.LogDebug("HolderInfo is {holder}",JsonConvert.SerializeObject(holder));
        }
        var nickNameByCaHashes = await GetNickNameByCaHashes(caHashes);
       
        var records = referralRecordList.Select(index => new ReferralRecordDetailDto
        {
            WalletName = nickNameByCaHashes.TryGetValue(index.CaHash, out var indexInfo) ? indexInfo.NickName : "",
            IsDirectlyInvite = index.IsDirectlyInvite == 0,
            ReferralDate = index.ReferralDate.ToString("yyyy-MM-dd"),
            Avatar = nickNameByCaHashes.TryGetValue(index.CaHash, out var caHolderIndex) ? caHolderIndex.Avatar : "",
        }).ToList();
        return new ReferralRecordResponseDto
        {
            HasNextPage = hasNextPage,
            ReferralRecords = records
        };
    }

    public async Task CalculateReferralRankAsync()
    {
        var endTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        var startTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() - 30000;
        var indexerReferralInfo =
            await _growthProvider.GetReferralInfoAsync(new List<string>(), new List<string>(),
                new List<string> { MethodName.CreateCAHolder }, startTime, endTime);
        if (indexerReferralInfo.ReferralInfo.IsNullOrEmpty() || indexerReferralInfo.ReferralInfo.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Time from {startTime} to {endTime} add total referral records count is {count}",
            ConvertTimestampToString(startTime) + "_" + startTime,
            ConvertTimestampToString(endTime) + "_" + endTime, indexerReferralInfo.ReferralInfo.Count);
        var inviteCodes = indexerReferralInfo.ReferralInfo.Select(t => t.ReferralCode).ToList();
        //need to be added score
        var growthInfos = await _growthProvider.GetGrowthInfosAsync(null, inviteCodes);
        foreach (var info in growthInfos)
        {
            _logger.LogDebug("GrowthInfo is {growth}", JsonConvert.SerializeObject(info));
        }

        var growthInfoDic = growthInfos.Where(t => !t.InviteCode.IsNullOrEmpty())
            .ToDictionary(t => t.InviteCode, t => t.CaHash);
        foreach (var indexer in indexerReferralInfo.ReferralInfo)
        {
            if (!growthInfoDic.ContainsKey(indexer.ReferralCode))
            {
                _logger.LogDebug("The data is not in dic,data is {data}", JsonConvert.SerializeObject(indexer));
                continue;
            }

            var caHolderInfo =
                await _activityProvider.GetCaHolderInfoAsync(new List<string>(),
                    growthInfoDic[indexer.ReferralCode]);

            var referralRecord = new ReferralRecordIndex
            {
                CaHash = indexer.CaHash,
                ReferralCode = indexer.ReferralCode,
                IsDirectlyInvite = 0,
                ReferralCaHash = growthInfoDic.TryGetValue(indexer.ReferralCode,out var referralCaHash)?referralCaHash : "",
                ReferralDate = UnixTimeStampToDateTime(indexer.Timestamp),
                ReferralAddress = caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress
            };
            var success = await _growthProvider.AddReferralRecordAsync(referralRecord);
            if (!success)
            {
                continue;
            }

            var score = await _cacheProvider.GetScoreAsync(CommonConstant.ReferralKey,
                caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress);
            await _cacheProvider.AddScoreAsync(CommonConstant.ReferralKey,
                caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress,
                score + 1);
            var scoreAdded = await _cacheProvider.GetScoreAsync(CommonConstant.ReferralKey,
                caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress);
            _logger.LogDebug("Sync Referral Date from index is {index},score is {score}",
                caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress, score);
        }
    }


    public async Task InitReferralRankAsync()
    {
        var skip = 0;
        var limit = 100;
        var count = 0;
        while (true)
        {
            var growthInfos = await _growthProvider.GetAllGrowthInfosAsync(skip, limit);
            if (growthInfos.IsNullOrEmpty())
            {
                break;
            }

            foreach (var index in growthInfos)
            {
                _logger.LogDebug("GrowthIndex from ES ,index is {index}", JsonConvert.SerializeObject(index));
            }

            foreach (var growthInfo in growthInfos)
            {
                var indexerReferralInfo =
                    await _growthProvider.GetReferralInfoAsync(new List<string>(),
                        new List<string> { growthInfo.InviteCode },
                        new List<string> { MethodName.CreateCAHolder }, 0, 0);
                if (indexerReferralInfo.ReferralInfo.IsNullOrEmpty() || indexerReferralInfo.ReferralInfo.Count == 0)
                {
                    _logger.LogDebug("Current CaHash is {caHash},Have no invite user.", growthInfo.CaHash);
                    continue;
                }

                var caHolderInfo =
                    await _activityProvider.GetCaHolderInfoAsync(new List<string>(), growthInfo.CaHash);
                _logger.LogDebug("CaHolder info is {info}",
                    JsonConvert.SerializeObject(caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress));

                foreach (var referralRecordIndex in indexerReferralInfo.ReferralInfo.Select(referralInfo =>
                             new ReferralRecordIndex
                             {
                                 CaHash = referralInfo.CaHash,
                                 ReferralCode = growthInfo.InviteCode,
                                 IsDirectlyInvite = 0,
                                 ReferralDate = UnixTimeStampToDateTime(referralInfo.Timestamp),
                                 ReferralCaHash = growthInfo.CaHash,
                                 ReferralAddress = caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress
                             }))
                {
                    _logger.LogDebug("Insert Referral detail is {detail}",
                        JsonConvert.SerializeObject(referralRecordIndex));
                    var success = await _growthProvider.AddReferralRecordAsync(referralRecordIndex);
                    if (success)
                    {
                        _logger.LogDebug("Begin Redis add score,key is {address}",
                            caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress);
                        await _cacheProvider.AddScoreAsync(CommonConstant.ReferralKey,
                            caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress,
                            indexerReferralInfo.ReferralInfo.Count);
                        var scoreAsync = await _cacheProvider.GetScoreAsync(CommonConstant.ReferralKey,
                            caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress);
                        _logger.LogDebug("GetScore from redis ,key is {key},score is {score}",
                            caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress, scoreAsync);
                    }

                    skip += limit;
                    count += growthInfos.Count;
                }
            }
        }

        _logger.LogDebug("Referral TotalCount is {count}", count);
    }

    public async Task<ReferralRecordsRankResponseDto> GetReferralRecordRankAsync(ReferralRecordRankRequestDto input)
    {
        var entries = await _cacheProvider.GetTopAsync(CommonConstant.ReferralKey, 0, 50);
        var sortedSetEntries = entries.Where(t => t.Score > 0).ToList();
        var list = new List<ReferralRecordsRankDetail>();
        foreach (var entry in sortedSetEntries)
        {
            var caAddress = entry.Element;
            var holderInfo =
                await _activityProvider.GetCaHolderInfoAsync(new List<string> { caAddress }, null);
            var caHash = holderInfo.CaHolderInfo.FirstOrDefault()?.CaHash;
            var caHolder = await _activityProvider.GetCaHolderAsync(caHash);
            _logger.LogDebug("Get caHolder is {caHolder},caAddress is {address},caHash is {caHash}",
                JsonConvert.SerializeObject(caHolder), caAddress, caHash);
            var rank = await _cacheProvider.GetRankAsync(CommonConstant.ReferralKey, entry.Element) + 1;
            var referralRecordsRankDetail = new ReferralRecordsRankDetail
            {
                Rank = Convert.ToInt16(rank),
                CaAddress = entry.Element,
                ReferralTotalCount = Convert.ToInt16(entry.Score),
                Avatar = caHolder != null ? caHolder.Avatar : "",
                WalletName = caHolder != null ? caHolder.NickName : ""
            };

            list.Add(referralRecordsRankDetail);
        }

        var currentUserReferralInfo = new ReferralRecordsRankDetail();
        //if (CurrentUser.Id.HasValue)
        if (!input.CaHash.IsNullOrEmpty())
        {
            //var caHolder = await _userAssetsProvider.GetCaHolderIndexAsync(CurrentUser.GetId());
            var caHolderInfo =
                await _activityProvider.GetCaHolderInfoAsync(new List<string>(), input.CaHash);
            var currentCaHolder = await _activityProvider.GetCaHolderAsync(input.CaHash);
            _logger.LogDebug("CurrentUser holder info is {info}", JsonConvert.SerializeObject(currentCaHolder));
            var currentRank = await _cacheProvider.GetRankAsync(CommonConstant.ReferralKey,
                caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress);
            var currentReferralCount = await _cacheProvider.GetScoreAsync(CommonConstant.ReferralKey,
                caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress);

            currentUserReferralInfo.Rank = Convert.ToInt16(currentRank) == 0
                ? Convert.ToInt16(currentRank)
                : Convert.ToInt16(currentRank) + 1;
            currentUserReferralInfo.ReferralTotalCount = Convert.ToInt16(currentReferralCount);
            currentUserReferralInfo.CaAddress = caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress;
            currentUserReferralInfo.Avatar = currentCaHolder != null ? currentCaHolder.Avatar : "";
        }
       
        var referralRecordRank = new ReferralRecordsRankResponseDto
        {
            ReferralRecordsRank = list,
            CurrentUserReferralRecordsRankDetail = currentUserReferralInfo
        };
        return referralRecordRank;
        //return new ReferralRecordsRankResponseDto();
    }

    private async Task<Dictionary<string, CAHolderIndex>> GetNickNameByCaHashes(List<string> caHashes)
    {
        var caHolderList = await GetCaHolderByCaHashAsync(caHashes);
        if (!caHolderList.IsNullOrEmpty())
        {
            foreach (var caHolder in caHolderList)
            {
                _logger.LogDebug("CaHolderInfo is {info}",JsonConvert.SerializeObject(caHolder));
            }
        }
        var caHashToWalletNameDic = caHolderList.Where(t => !t.CaHash.IsNullOrEmpty())
            .ToDictionary(t => t.CaHash, t => t);
        return caHashToWalletNameDic;
    }

    private async Task SetOriginAsync(ReferralResponseDto responseDto)
    {
        var caHashes = responseDto.ReferralInfos.Select(t => t.CaHash).ToList();
        var indexerReferralInfos =
            await _growthProvider.GetReferralInfoAsync(caHashes, new List<string>(),
                new List<string> { MethodName.CreateCAHolder }, 0, 0);

        if (indexerReferralInfos == null || indexerReferralInfos.ReferralInfo.IsNullOrEmpty())
        {
            return;
        }

        foreach (var referralInfo in indexerReferralInfos.ReferralInfo)
        {
            var referral = responseDto.ReferralInfos.First(t => t.CaHash == referralInfo.CaHash);
            referral.ReferralCode = referralInfo.ReferralCode;
        }

        var referralCodes = indexerReferralInfos.ReferralInfo.Select(t => t.ReferralCode).ToList();
        var growthInfos = await _growthProvider.GetGrowthInfosAsync(null, referralCodes);
        if (growthInfos.IsNullOrEmpty())
        {
            return;
        }

        var caHashesParam = new List<string>();
        foreach (var growthInfo in growthInfos)
        {
            var referral = responseDto.ReferralInfos.FirstOrDefault(t => t.ReferralCode == growthInfo.InviteCode);
            if (referral == null) continue;

            var refs = new Referral
            {
                CaHash = growthInfo.CaHash,
                ProjectCode = growthInfo.ProjectCode,
                InviteCode = growthInfo.InviteCode,

                Children = new List<Referral>() { referral }
            };

            caHashesParam.Add(growthInfo.CaHash);
            responseDto.ReferralInfos.Remove(referral);
            responseDto.ReferralInfos.Add(refs);
        }

        await SetOriginAsync(responseDto);
    }

    // who i invited
    public async Task<ReferralResponseDto> GetReferralInfoTreeAsync(ReferralRequestDto input)
    {
        var result = new ReferralResponseDto();

        foreach (var caHash in input.CaHashes)
        {
            result.ReferralInfos.Add(new Referral()
            {
                CaHash = caHash
            });
        }

        var growthInfos = await _growthProvider.GetGrowthInfosAsync(input.CaHashes, null);
        if (growthInfos.IsNullOrEmpty())
        {
            return result;
        }

        foreach (var growthInfo in growthInfos)
        {
            var referralInfo = result.ReferralInfos.First(t => t.CaHash == growthInfo.CaHash);
            referralInfo.ProjectCode = growthInfo.ProjectCode;
            referralInfo.InviteCode = growthInfo.InviteCode;
        }

        var indexerReferralInfo =
            await _growthProvider.GetReferralInfoAsync(input.CaHashes, new List<string>(),
                new List<string> { MethodName.CreateCAHolder }, 0, 0);

        foreach (var referralInfo in indexerReferralInfo.ReferralInfo)
        {
            var referral = result.ReferralInfos.First(t => t.CaHash == referralInfo.CaHash);
            referral.ReferralCode = referralInfo.ReferralCode;
        }

        await GetReferralInfoListAsync(result.ReferralInfos);
        return result;
    }

    private async Task GetReferralInfoListAsync(List<Referral> referralInfos)
    {
        if (referralInfos.IsNullOrEmpty()) return;

        var caHashes = referralInfos.Select(t => t.CaHash).ToList();
        var growthInfos = await _growthProvider.GetGrowthInfosAsync(caHashes, null);
        if (growthInfos.IsNullOrEmpty()) return;

        foreach (var growthInfo in growthInfos)
        {
            var referral = referralInfos.First(t => t.CaHash == growthInfo.CaHash);
            referral.InviteCode = growthInfo.InviteCode;
        }

        var inviteCodes = growthInfos.Select(t => t.InviteCode).ToList();
        var indexerReferralInfos =
            await _growthProvider.GetReferralInfoAsync(new List<string>(), inviteCodes,
                new List<string> { MethodName.CreateCAHolder }, 0, 0);

        if (indexerReferralInfos.ReferralInfo.IsNullOrEmpty())
        {
            return;
        }

        foreach (var referralInfo in indexerReferralInfos.ReferralInfo)
        {
            var referral = referralInfos.First(t => t.InviteCode == referralInfo.ReferralCode);
            referral.Children.Add(new Referral()
            {
                CaHash = referralInfo.CaHash,
                ProjectCode = referralInfo.ProjectCode,
                ReferralCode = referralInfo.ReferralCode
            });
        }

        var children = referralInfos.SelectMany(t => t.Children).ToList();
        await GetReferralInfoListAsync(children);
    }

    private async Task<List<CAHolderIndex>> GetCaHolderByCaHashAsync(List<string> caHashList)
    {
        if (caHashList == null || caHashList.Count == 0)
        {
            return new List<CAHolderIndex>();
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<CAHolderIndex>, QueryContainer>>() { };

        mustQuery.Add(q => q.Terms(i => i.Field(f => f.CaHash).Terms(caHashList)));
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var holders = await _caHolderRepository.GetListAsync(Filter);

        return holders.Item2;
    }

    private static string ConvertTimestampToString(long timestamp)
    {
        DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        return dateTime.ToString("G");
    }

    private static string GetSpecifyDay(int n)
    {
        var specifyDay = DateTime.Now.AddDays(n);
        return specifyDay.ToString("yyyy-MM-dd");
    }

    private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dtDateTime;
    }
}
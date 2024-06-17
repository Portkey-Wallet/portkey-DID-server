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
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Growth;

[RemoteService(false), DisableAuditing]
public class GrowthStatisticAppService : CAServerAppService, IGrowthStatisticAppService
{
    private readonly IGrowthProvider _growthProvider;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    //private readonly ICacheProvider _cacheProvider;
    private readonly IActivityProvider _activityProvider;
    private readonly ILogger<GrowthStatisticAppService> _logger;


    public GrowthStatisticAppService(IGrowthProvider growthProvider,
        INESTRepository<CAHolderIndex, Guid> caHolderRepository,
        //ICacheProvider cacheProvider, 
        IActivityProvider activityProvider, ILogger<GrowthStatisticAppService> logger)
    {
        _growthProvider = growthProvider;
        _caHolderRepository = caHolderRepository;
        //_cacheProvider = cacheProvider;
        _activityProvider = activityProvider;
        _logger = logger;
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
        var caHash = input.CaHash;
        var growthInfo = await _growthProvider.GetGrowthInfoByCaHashAsync(caHash);
        if (growthInfo == null)
        {
            return 0;
        }

        // Get First invite users
        var indexerReferralInfo =
            await _growthProvider.GetReferralInfoAsync(new List<string>(), new List<string> { growthInfo.InviteCode },
                new List<string> { MethodName.CreateCAHolder }, 0, 0);

        //Get Second invite users
        var caHashes = indexerReferralInfo.ReferralInfo.Select(t => t.CaHash).ToList();
        var growthInfos = await _growthProvider.GetGrowthInfosAsync(caHashes, null);
        if (growthInfos.IsNullOrEmpty())
        {
            return indexerReferralInfo.ReferralInfo.Count;
        }

        var inviteCodes = growthInfos.Select(t => t.InviteCode).ToList();
        var secondReferralUsers =
            await _growthProvider.GetReferralInfoAsync(new List<string>(), inviteCodes,
                new List<string> { MethodName.CreateCAHolder }, 0, 0);
        return indexerReferralInfo.ReferralInfo.Count + secondReferralUsers.ReferralInfo.Count;
    }

    public async Task<ReferralRecordResponseDto> GetReferralRecordList(ReferralRecordRequestDto input)
    {
        var caHash = input.CaHash;

        var referralRecordList =
            await _growthProvider.GetReferralRecordListAsync(null, caHash, input.Skip, input.Limit);
        var caHashes = referralRecordList.Select(t => t.CaHash).ToList();
        var nickNameByCaHashes = await GetNickNameByCaHashes(caHashes);
        var records = referralRecordList.Select(index => new ReferralRecordDetailDto
        {
            WalletName = nickNameByCaHashes[index.CaHash],
            IsDirectlyInvite = index.IsDirectlyInvite == 0,
            ReferralDate = index.CreateTime.ToString("yyyy-MM-dd")
        }).ToList();
        var lastDayCount = GetLastdayTotalCount(referralRecordList);
        return new ReferralRecordResponseDto()
        {
            LastDayReferralTotalCount = lastDayCount,
            ReferralRecords = records
        };
    }

    public async Task CalculateReferralRankAsync()
    {
        var endTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        var startTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() - 4000;
        var skip = 0;
        var limit = 100;
        while (true)
        {
            var indexerReferralInfo =
                await _growthProvider.GetReferralInfoAsync(new List<string>(), new List<string>(),
                    new List<string> { MethodName.CreateCAHolder }, startTime, endTime);
            if (indexerReferralInfo.ReferralInfo.IsNullOrEmpty())
            {
                break;
            }

            _logger.LogDebug("Time from {startTime} to {endTime} add total referral records count is {count}",
                ConvertTimestampToString(startTime),
                ConvertTimestampToString(endTime), indexerReferralInfo.ReferralInfo.Count);
            var inviteCodes = indexerReferralInfo.ReferralInfo.Select(t => t.ReferralCode).ToList();
            //need to be added score
            var growthInfos = await _growthProvider.GetGrowthInfosAsync(null, inviteCodes);
            // first level to be added 
            var referralCodes = growthInfos.Select(t => t.ReferralCode).ToList();

            var growthInfoDic = growthInfos.ToDictionary(t => t.InviteCode, t => t.CaHash);
            foreach (var indexer in indexerReferralInfo.ReferralInfo)
            {
                var caHolderInfo =
                    await _activityProvider.GetCaHolderInfoAsync(new List<string>(),
                        growthInfoDic[indexer.ReferralCode]);
                // var score = await _cacheProvider.GetScoreAsync(CommonConstant.ReferralKey,
                //     caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress);
                // await _cacheProvider.AddScoreAsync(CommonConstant.ReferralKey,
                //     caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress,
                //     score + 1);
                var referralRecord = new ReferralRecordIndex
                {
                    CaHash = indexer.CaHash,
                    ReferralCode = indexer.ReferralCode,
                    IsDirectlyInvite = 0,
                    ReferralCaHash = growthInfoDic[indexer.ReferralCode],
                    CreateTime = UnixTimeStampToDateTime(indexer.Timestamp),
                    ReferralAddress = caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress
                };
                await _growthProvider.AddReferralRecordAsync(referralRecord);
            }

            var secondGrowthInfos = await _growthProvider.GetGrowthInfosAsync(null, referralCodes);
            var secondGrowthInfoDic = growthInfos.ToDictionary(t => t.InviteCode, t => t.CaHash);
            foreach (var growthIndex in secondGrowthInfos)
            {
                var caHolderInfo =
                    await _activityProvider.GetCaHolderInfoAsync(new List<string>(),
                        secondGrowthInfoDic[growthIndex.ReferralCode]);

                // var score = await _cacheProvider.GetScoreAsync(CommonConstant.ReferralKey,
                //     caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress);
                // await _cacheProvider.AddScoreAsync(CommonConstant.ReferralKey,
                //     caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress,
                //     score + 1);

                var referralRecord = new ReferralRecordIndex
                {
                    CaHash = growthIndex.CaHash,
                    ReferralCode = growthIndex.ReferralCode,
                    IsDirectlyInvite = 1,
                    ReferralCaHash = secondGrowthInfoDic[growthIndex.ReferralCode],
                    CreateTime = growthIndex.CreateTime,
                    ReferralAddress = caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress
                };
                await _growthProvider.AddReferralRecordAsync(referralRecord);
            }
            skip += limit;
        }
    }


    public async Task InitReferralRankAsync()
    {
        var skip = 0;
        var limit = 100;
        var count = 0;
        while (true)
        {
            var list = await _growthProvider.GetAllGrowthInfosAsync(skip, limit);
            if (list.IsNullOrEmpty())
            {
                break;
            }

            var caHashes = list.Select(t => t.CaHash).ToList();
            foreach (var caHash in caHashes)
            {
                var growthInfo = await _growthProvider.GetGrowthInfoByCaHashAsync(caHash);
                if (growthInfo == null)
                {
                    continue;
                }

                _logger.LogDebug("Current CaHash is {caHash},Referral detail is {referral}", caHash,
                    JsonConvert.SerializeObject(growthInfo));
                //First invite
                var indexerReferralInfo =
                    await _growthProvider.GetReferralInfoAsync(new List<string>(), new List<string> { growthInfo.InviteCode },
                        new List<string> { MethodName.CreateCAHolder }, 0, 0);
                if (indexerReferralInfo.ReferralInfo.IsNullOrEmpty())
                {
                    continue;
                }

                var caHolderInfo =
                    await _activityProvider.GetCaHolderInfoAsync(new List<string>(), caHash);
                //add record to ES
                foreach (var referralRecordIndex in indexerReferralInfo.ReferralInfo.Select(referralInfo =>
                             new ReferralRecordIndex
                             {
                                 CaHash = referralInfo.CaHash,
                                 ReferralCode = growthInfo.InviteCode,
                                 IsDirectlyInvite = 0,
                                 CreateTime = UnixTimeStampToDateTime(referralInfo.Timestamp),
                                 ReferralCaHash = caHash,
                                 ReferralAddress = caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress
                             }))
                {
                    _logger.LogDebug("Insert first level  Referral detail is {detail}",JsonConvert.SerializeObject(referralRecordIndex));
                    await _growthProvider.AddReferralRecordAsync(referralRecordIndex);
                }

                var secondCaHashes = indexerReferralInfo.ReferralInfo.Select(t => t.CaHash).ToList();
                var growthIndices = await _growthProvider.GetGrowthInfosAsync(secondCaHashes, null);
                var inviteCodes = growthIndices.Select(t => t.InviteCode).ToList();
                var referralInfoDto = await _growthProvider.GetReferralInfoAsync(new List<string>(), new List<string>(inviteCodes),
                    new List<string>
                    {
                        MethodName.CreateCAHolder
                    }, 0, 0);

                if (!referralInfoDto.ReferralInfo.IsNullOrEmpty())
                {
                    foreach (var referralRecordIndex in referralInfoDto.ReferralInfo.Select(indexer =>
                                 new ReferralRecordIndex
                                 {
                                     CaHash = indexer.CaHash,
                                     ReferralCode = indexer.ReferralCode,
                                     IsDirectlyInvite = 1,
                                     CreateTime = UnixTimeStampToDateTime(indexer.Timestamp),
                                     ReferralCaHash = caHash,
                                     ReferralAddress = caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress
                                 }))
                    {
                        _logger.LogDebug("Insert second level  Referral detail is {detail}",
                            JsonConvert.SerializeObject(referralRecordIndex));
                        await _growthProvider.AddReferralRecordAsync(referralRecordIndex);
                    }
                }

                // await _cacheProvider.AddScoreAsync(CommonConstant.ReferralKey,
                //     caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress,
                //     indexerReferralInfo.ReferralInfo.Count + referralInfoDto.ReferralInfo.Count);
                skip += limit;
                count += list.Count;
            }
        }

        _logger.LogDebug("Referral TotalCount is {count}", count);
    }

    public async Task<ReferralRecordsRankResponseDto> GetReferralRecordRankAsync(ReferralRecordRankRequestDto input)
    {
        // var entries = await _cacheProvider.GetTopAsync(CommonConstant.ReferralKey, 0, 50);
        // var list = new List<ReferralRecordsRankDetail>();
        // foreach (var entry in entries)
        // {
        //     var referralRecordsRankDetail = new ReferralRecordsRankDetail
        //     {
        //         CaAddress = entry.Element,
        //         ReferralTotalCount = Convert.ToInt16(entry.Score)
        //     };
        //
        //     list.Add(referralRecordsRankDetail);
        // }
        //
        // var caHolderInfo =
        //     await _activityProvider.GetCaHolderInfoAsync(new List<string>(), input.CaHash);
        //
        // var totalCount = await _cacheProvider.GetRankAsync(CommonConstant.ReferralKey,
        //     caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress);
        // var referralRecordRank = new ReferralRecordsRankResponseDto
        // {
        //     ReferralRecordsRank = list,
        //     CurrentUserReferralRecordsRankDetail = new ReferralRecordsRankDetail
        //     {
        //         ReferralTotalCount = Convert.ToInt16(totalCount),
        //         CaAddress = caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress
        //     }
        // };
        //return referralRecordRank;
        return new ReferralRecordsRankResponseDto();
    }

    private int GetLastdayTotalCount(List<ReferralRecordIndex> records)
    {
        var dateTimeToRecordDic = records.GroupBy(record => record.CreateTime.ToString("yyyy-MM-dd"))
            .ToDictionary(record => record.Key, record => record.ToList());
        var yesterdayReferralRecords = dateTimeToRecordDic[GetSpecifyDay(-1)];
        return yesterdayReferralRecords.Count;
    }

    private async Task<Dictionary<string, string>> GetNickNameByCaHashes(List<string> caHashes)
    {
        var caHolderList = await GetCaHolderByCaHashAsync(caHashes);
        var caHashToWalletNameDic = caHolderList.ToDictionary(t => t.CaHash, t => t.NickName);
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
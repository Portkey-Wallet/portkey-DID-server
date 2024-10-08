using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Indexing.Elasticsearch;
using CAServer.CAActivity.Provider;
using CAServer.Cache;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.Grains;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.Guardian;
using CAServer.Growth.Dtos;
using CAServer.Growth.Provider;
using CAServer.Options;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization;
using Volo.Abp.Users;
using Volo.Abp.Validation;
using Result = CAServer.Growth.Dtos.Result;

namespace CAServer.Growth;

[RemoteService(false), DisableAuditing]
public class GrowthStatisticAppService : CAServerAppService, IGrowthStatisticAppService
{
    private readonly IGrowthProvider _growthProvider;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly ICacheProvider _cacheProvider;
    private readonly IActivityProvider _activityProvider;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ILogger<GrowthStatisticAppService> _logger;
    private readonly IUserAssetsProvider _userAssetsProvider;
    private const int RankLimit = 50;
    private const string ReferralCalculateTimesCache = "Portkey:ReferralCalculateTimesCache";
    private const int ExpireTime = 360;
    private readonly ActivityConfigOptions _activityConfigOptions;
    private readonly HamsterOptions _hamsterOptions;
    private readonly BeInvitedConfigOptions _beInvitedConfigOptions;
    private const string RepairDataCache = "Hamster:DataRepairKey";
    private const string HamsterTonGiftsUserIdsKey = "Hamster:TonGifts:UserIdsKey";
    private readonly IClusterClient _clusterClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TonGiftsOptions _tonGiftsOptions;
    private readonly IGuardianAppService _guardianAppService;


    public GrowthStatisticAppService(IGrowthProvider growthProvider,
        INESTRepository<CAHolderIndex, Guid> caHolderRepository,
        ICacheProvider cacheProvider,
        IActivityProvider activityProvider, IGraphQLProvider _graphQlProvider, ILogger<GrowthStatisticAppService> logger,
        IUserAssetsProvider userAssetsProvider, IOptionsSnapshot<ActivityConfigOptions> activityConfigOptions,
        IOptionsSnapshot<HamsterOptions> hamsterOptions,
        IOptionsSnapshot<BeInvitedConfigOptions> beInvitedConfigOptions, IClusterClient clusterClient,
        IHttpClientFactory httpClientFactory, IOptionsSnapshot<TonGiftsOptions> tonGiftsOptions, IGuardianAppService guardianAppService)
    {
        _growthProvider = growthProvider;
        _caHolderRepository = caHolderRepository;
        _cacheProvider = cacheProvider;
        _activityProvider = activityProvider;
        _graphQlProvider = _graphQlProvider;
        _logger = logger;
        _userAssetsProvider = userAssetsProvider;
        _clusterClient = clusterClient;
        _httpClientFactory = httpClientFactory;
        _tonGiftsOptions = tonGiftsOptions.Value;
        _beInvitedConfigOptions = beInvitedConfigOptions.Value;
        _hamsterOptions = hamsterOptions.Value;
        _activityConfigOptions = activityConfigOptions.Value;
        _guardianAppService = guardianAppService;
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
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Unauthorized.");
        }

        var caHolder = await _userAssetsProvider.GetCaHolderIndexAsync(CurrentUser.GetId());
        var growthInfo = await _growthProvider.GetReferralRecordListAsync(null, caHolder.CaHash, 0,
            Int16.MaxValue, null, null, new List<int> { 0 });
        return growthInfo?.Count ?? 0;
    }

    public async Task<ReferralRecordResponseDto> GetReferralRecordList(ReferralRecordRequestDto input)
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Unauthorized.");
        }

        var caHolder = await _userAssetsProvider.GetCaHolderIndexAsync(CurrentUser.GetId());
        var hasNextPage = true;
        var details = GetActivityDetails(input.ActivityEnums);

        var referralRecordList = input.ActivityEnums switch
        {
            ActivityEnums.Invitation => await _growthProvider.GetReferralRecordListAsync(null, caHolder.CaHash,
                input.Skip, input.Limit, null, null, new List<int> { 0 }),
            ActivityEnums.Hamster => await _growthProvider.GetReferralRecordListAsync(null, caHolder.CaHash, input.Skip,
                input.Limit, Convert.ToDateTime(details.StartDate), Convert.ToDateTime(details.EndDate),
                new List<int> { 0, 1 }),
            _ => throw new UserFriendlyException("Invalidate Activity.")
        };
        if (referralRecordList.Count < input.Limit)
        {
            hasNextPage = false;
        }

        var caHashes = referralRecordList.Select(t => t.CaHash).Distinct().ToList();
        var nickNameByCaHashes = await GetNickNameByCaHashes(caHashes);
        var hamsterDesc = String.Format(CommonConstant.HamsterScore, _hamsterOptions.MinAcornsScore);

        var records = new List<ReferralRecordDetailDto>();
        foreach (var index in referralRecordList)
        {
            var record = new ReferralRecordDetailDto();
            var walletName = nickNameByCaHashes.TryGetValue(index.CaHash, out var indexInfo)
                ? indexInfo.NickName
                : "";
            var recordDesc = walletName;
            if (index.ReferralType == 0)
            {
                recordDesc += CommonConstant.SingUp;
            }
            else
            {
                recordDesc += hamsterDesc;
            }

            record.WalletName = walletName;
            record.RecordDesc = recordDesc;
            record.Avatar = nickNameByCaHashes.TryGetValue(index.CaHash, out var caHolderIndex)
                ? caHolderIndex.Avatar
                : "";
            ;
            record.IsDirectlyInvite = index.IsDirectlyInvite == 0;
            record.ReferralDate = index.ReferralDate.ToString("yyyy-MM-dd");
            records.Add(record);
        }

        return new ReferralRecordResponseDto
        {
            HasNextPage = hasNextPage,
            ReferralRecords = records
        };
    }

    public async Task CalculateReferralRankAsync()
    {
        var startTime = 0L;
        var referralTimes = await _cacheProvider.Get(ReferralCalculateTimesCache);
        if (!referralTimes.HasValue)
        {
            startTime = StringToTimeStamp(CommonConstant.DefaultReferralActivityStartTime);
        }
        else
        {
            startTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() - 30000;
        }

        var endTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

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

        var growthInfoDic = growthInfos.Where(t => !t.InviteCode.IsNullOrEmpty())
            .ToDictionary(t => t.InviteCode, t => t.CaHash);
        foreach (var indexer in indexerReferralInfo.ReferralInfo)
        {
            if (!growthInfoDic.ContainsKey(indexer.ReferralCode))
            {
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
                ReferralCaHash = growthInfoDic.TryGetValue(indexer.ReferralCode, out var referralCaHash)
                    ? referralCaHash
                    : "",
                ReferralDate = UnixTimeStampToDateTime(indexer.Timestamp),
                ReferralAddress = caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress,
                ReferralType = 0
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
        }

        var expire = TimeSpan.FromDays(ExpireTime);
        await _cacheProvider.Set(ReferralCalculateTimesCache, "Init", expire);
    }


    public async Task<ReferralRecordsRankResponseDto> GetReferralRecordRankAsync(ReferralRecordRankRequestDto input)
    {
        var referralRecordRank = new ReferralRecordsRankResponseDto();
        referralRecordRank = input.ActivityEnums switch
        {
            ActivityEnums.Invitation => await BuildInvitationRankAsync(input, referralRecordRank),
            ActivityEnums.Hamster => await BuildHamsterRankAsync(input, referralRecordRank),
            _ => throw new UserFriendlyException("Invalidate Activity.")
        };

        var currentUserReferralInfo = new ReferralRecordsRankDetail();
        if (CurrentUser.Id.HasValue)
        {
            var caHolder = await _userAssetsProvider.GetCaHolderIndexAsync(CurrentUser.GetId());
            var caHolderInfo =
                await _activityProvider.GetCaHolderInfoAsync(new List<string>(), caHolder.CaHash);
            var currentCaHolder = await _activityProvider.GetCaHolderAsync(caHolder.CaHash);
            _logger.LogDebug("CurrentUser holder info is {info}", JsonConvert.SerializeObject(currentCaHolder));
            var currentRank = input.ActivityEnums switch
            {
                ActivityEnums.Invitation => await _cacheProvider.GetRankAsync(CommonConstant.ReferralKey,
                    caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress),
                ActivityEnums.Hamster => await _cacheProvider.GetRankAsync(CommonConstant.HamsterRankKey,
                    caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress),
                _ => throw new UserFriendlyException("Invalidate Activity.")
            };
            if (currentRank == -1)
            {
                currentUserReferralInfo.Rank = 0;
                currentUserReferralInfo.ReferralTotalCount = 0;
                currentUserReferralInfo.CaAddress = caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress;
                currentUserReferralInfo.Avatar = currentCaHolder != null ? currentCaHolder.Avatar : "";
                currentUserReferralInfo.WalletName = currentCaHolder != null ? currentCaHolder.NickName : "";
            }
            else
            {
                List<double> scoreList;
                var currentReferralCount = 0d;
                switch (input.ActivityEnums)
                {
                    case ActivityEnums.Invitation:
                        var sortedEntries =
                            await _cacheProvider.GetTopAsync(CommonConstant.ReferralKey, 0, currentRank + 1);
                        scoreList = sortedEntries.Select(t => t.Score).ToList();
                        currentReferralCount = await _cacheProvider.GetScoreAsync(CommonConstant.ReferralKey,
                            caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress);
                        break;
                    case ActivityEnums.Hamster:
                        var hamsterSortedEntries =
                            await _cacheProvider.GetTopAsync(CommonConstant.HamsterRankKey, 0, currentRank + 1);
                        scoreList = hamsterSortedEntries.Select(t => t.Score).ToList();
                        currentReferralCount = await _cacheProvider.GetScoreAsync(CommonConstant.HamsterRankKey,
                            caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress);
                        break;
                    default:
                        throw new UserFriendlyException("Invalidate Activity.");
                }

                scoreList.Sort();
                scoreList.Reverse();
                currentUserReferralInfo.Rank = scoreList.IndexOf(currentReferralCount) + 1;
                currentUserReferralInfo.ReferralTotalCount = Convert.ToInt16(currentReferralCount);
                currentUserReferralInfo.CaAddress = caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress;
                currentUserReferralInfo.Avatar = currentCaHolder != null ? currentCaHolder.Avatar : "";
                currentUserReferralInfo.WalletName = currentCaHolder != null ? currentCaHolder.NickName : "";
            }
        }

        referralRecordRank.CurrentUserReferralRecordsRankDetail = currentUserReferralInfo;
        referralRecordRank.Invitations = _hamsterOptions.Invitations;
        return referralRecordRank;
    }

    private async Task<ReferralRecordsRankResponseDto> BuildHamsterRankAsync(ReferralRecordRankRequestDto input,
        ReferralRecordsRankResponseDto response)
    {
        var hasNext = true;
        var list = new List<ReferralRecordsRankDetail>();
        var length = await _cacheProvider.GetSortedSetLengthAsync(CommonConstant.HamsterRankKey);
        var entries = await _cacheProvider.GetTopAsync(CommonConstant.HamsterRankKey, 0, input.Skip + input.Limit - 1);
        if (length <= input.Skip + input.Limit)
        {
            hasNext = false;
        }

        var scores = entries.Select(t => t.Score).ToList();
        scores.Sort();
        scores.Reverse();
        var skipList = entries.Skip(input.Skip).Take(input.Limit).ToArray();
        foreach (var entry in skipList)
        {
            var caAddress = entry.Element;
            var holderInfo =
                await _activityProvider.GetCaHolderInfoAsync(new List<string> { caAddress }, null);
            var caHash = holderInfo.CaHolderInfo.FirstOrDefault()?.CaHash;
            var caHolder = await _activityProvider.GetCaHolderAsync(caHash);
            _logger.LogDebug("Get caHolder is {caHolder},caAddress is {address},caHash is {caHash}",
                JsonConvert.SerializeObject(caHolder), caAddress, caHash);
            var score = await _cacheProvider.GetScoreAsync(CommonConstant.HamsterRankKey, entry.Element);
            if (scores.IndexOf(score) + 1 > RankLimit)
            {
                hasNext = false;
                break;
            }

            var referralRecordsRankDetail = new ReferralRecordsRankDetail
            {
                Rank = scores.IndexOf(score) + 1,
                CaAddress = entry.Element,
                ReferralTotalCount = Convert.ToInt16(entry.Score),
                Avatar = caHolder != null ? caHolder.Avatar : "",
                WalletName = caHolder != null ? caHolder.NickName : ""
            };
            list.Add(referralRecordsRankDetail);
        }

        response.HasNext = hasNext;
        response.ReferralRecordsRank = list;
        return response;
    }

    private async Task<ReferralRecordsRankResponseDto> BuildInvitationRankAsync(ReferralRecordRankRequestDto input,
        ReferralRecordsRankResponseDto response)
    {
        var hasNext = true;
        var list = new List<ReferralRecordsRankDetail>();
        var length = await _cacheProvider.GetSortedSetLengthAsync(CommonConstant.ReferralKey);
        var entries = await _cacheProvider.GetTopAsync(CommonConstant.ReferralKey, 0, input.Skip + input.Limit - 1);
        if (length <= input.Skip + input.Limit)
        {
            hasNext = false;
        }

        var scores = entries.Select(t => t.Score).ToList();
        scores.Sort();
        scores.Reverse();
        var skipList = entries.Skip(input.Skip).Take(input.Limit).ToArray();
        foreach (var entry in skipList)
        {
            var caAddress = entry.Element;
            var holderInfo =
                await _activityProvider.GetCaHolderInfoAsync(new List<string> { caAddress }, null);
            var caHash = holderInfo.CaHolderInfo.FirstOrDefault()?.CaHash;
            var caHolder = await _activityProvider.GetCaHolderAsync(caHash);
            _logger.LogDebug("Get caHolder is {caHolder},caAddress is {address},caHash is {caHash}",
                JsonConvert.SerializeObject(caHolder), caAddress, caHash);
            var score = await _cacheProvider.GetScoreAsync(CommonConstant.ReferralKey, entry.Element);
            if (scores.IndexOf(score) + 1 > RankLimit)
            {
                hasNext = false;
                break;
            }

            var referralRecordsRankDetail = new ReferralRecordsRankDetail
            {
                Rank = scores.IndexOf(score) + 1,
                CaAddress = entry.Element,
                ReferralTotalCount = Convert.ToInt16(entry.Score),
                Avatar = caHolder != null ? caHolder.Avatar : "",
                WalletName = caHolder != null ? caHolder.NickName : ""
            };
            list.Add(referralRecordsRankDetail);
        }

        response.HasNext = hasNext;
        response.ReferralRecordsRank = list;
        return response;
    }

    public async Task CalculateHamsterDataAsync()
    {
        var details = GetActivityConfig(ActivityEnums.Hamster);
        var startTime = Convert.ToDateTime(details.StartDate);
        var endTime = DateTime.UtcNow;
        if (new DateTimeOffset(endTime).ToUnixTimeSeconds() >
            new DateTimeOffset(Convert.ToDateTime(details.EndDate)).ToUnixTimeSeconds())
        {
            _logger.LogDebug("Current activity has been ended.");
            return;
        }

        var referralRecordList =
            await _growthProvider.GetReferralRecordListAsync(null, null, 0, PagedResultRequestDto.MaxMaxResultCount, startTime, endTime,
                new List<int> { 0 });
        var list = referralRecordList.Where(t => t.ReferralType == 0).ToList();
        if (list.IsNullOrEmpty() || list.Count == 0)
        {
            _logger.LogDebug("Hamster Referral data from ES is null.");
            return;
        }

        var recordGroup =
            list.GroupBy(t => t.ReferralCaHash);
        foreach (var group in recordGroup)
        {
            var hamsterReferralInfo = new Dictionary<string, string>();
            var referralRecords = group.ToList();
            var hamsterReferralDic = referralRecords.ToDictionary(t => t.CaHash, k => k);
            var caHash = group.Key;
            var addresses = await GetHamsterReferralAddressAsync(referralRecords, hamsterReferralInfo);

            var result = new List<HamsterScoreDto>();

            if (addresses.Count >= 100)
            {
                var index = 0;
                var length = 50;
                while (true)
                {
                    var subList = addresses.Skip(index).Take(length).ToList();
                    if (subList.Count <= 0)
                    {
                        break;
                    }

                    var hamsterScoreList =
                        await _growthProvider.GetHamsterScoreListAsync(subList, startTime, endTime);
                    var scoreResult = hamsterScoreList.GetScoreInfos
                        .Where(t => t.SumScore / 100000000 >= _hamsterOptions.MinAcornsScore).ToList();
                    if (!scoreResult.IsNullOrEmpty())
                    {
                        result.AddRange(scoreResult);
                    }

                    index += length;
                }
            }
            else
            {
                var hamsterScoreList = await _growthProvider.GetHamsterScoreListAsync(addresses, startTime, endTime);
                result = hamsterScoreList.GetScoreInfos
                    .Where(t => t.SumScore / 100000000 >= _hamsterOptions.MinAcornsScore).ToList();
            }

            if (result.IsNullOrEmpty())
            {
                _logger.LogDebug("No scores over limit.");
                continue;
            }

            var caHolderInfo =
                await _activityProvider.GetCaHolderInfoAsync(new List<string>(),
                    caHash);
            await _cacheProvider.AddScoreAsync(CommonConstant.HamsterRankKey,
                caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress, result.Count);

            foreach (var hamster in result)
            {
                var address = hamster.CaAddress.Split("_")[1];
                var record =
                    await _growthProvider.GetReferralRecordListAsync(
                        hamsterReferralInfo[address], caHash, 0,
                        1,
                        null, null, new List<int> { 1 });
                if (!record.IsNullOrEmpty())
                {
                    continue;
                }

                var referralCaHash = hamsterReferralInfo[address];
                var index = new ReferralRecordIndex
                {
                    CaHash = referralCaHash,
                    ReferralCode = hamsterReferralDic[referralCaHash].ReferralCode,
                    IsDirectlyInvite = 0,
                    ReferralCaHash = caHash,
                    ReferralDate = DateTime.UtcNow,
                    ReferralAddress = hamsterReferralDic[referralCaHash].ReferralAddress,
                    ReferralType = 1
                };
                await _growthProvider.AddReferralRecordAsync(index);
            }
        }
    }

    private async Task<List<string>> GetHamsterReferralAddressAsync(List<ReferralRecordIndex> referralRecords,
        Dictionary<string, string> userInfoDic)
    {
        var addresses = new List<string>();
        foreach (var index in referralRecords)
        {
            var holderInfo =
                await _activityProvider.GetCaHolderInfoAsync(new List<string> { }, index.CaHash);
            var address = holderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress;
            if (string.IsNullOrEmpty(address))
            {
                continue;
            }

            var formatAddress = _hamsterOptions.AddressPrefix + address + _hamsterOptions.AddressSuffix;
            addresses.Add(formatAddress);
            userInfoDic.Add(address, holderInfo.CaHolderInfo.FirstOrDefault()?.CaHash);
        }

        return addresses;
    }

    public async Task<RewardProgressResponseDto> GetRewardProgressAsync(ActivityEnums activityEnum)
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Unauthorized.");
        }

        var caHolder = await _userAssetsProvider.GetCaHolderIndexAsync(CurrentUser.GetId());
        var details = GetActivityDetails(activityEnum);
        var data = new RewardProgressResponseDto();
        switch (activityEnum)
        {
            case ActivityEnums.Invitation:
                var invitationCount = await GetReferralTotalCountAsync(new ReferralRecordRequestDto());
                var invitationDto = new InvitationRewardProgressDto
                {
                    SignUpCount = invitationCount
                };
                var progressList = ModelToDictionary(invitationDto);
                data.Data = progressList;
                data.RewardProcessCount = "";
                return data;
            case ActivityEnums.Hamster:
            {
                HamsterRewardProgressDto hamsterProgress;
                var referralList =
                    await _growthProvider.GetReferralRecordListAsync(caHolder.CaHash, null, 0, PagedResultRequestDto.MaxMaxResultCount,
                        Convert.ToDateTime(details.StartDate), Convert.ToDateTime(details.EndDate),
                        new List<int> { 1 });
                var reward = "";
                var indexes = await GetHamsterSignUpCount(caHolder.CaHash, details.StartDate, details.EndDate);
                if (indexes.Count == 0)
                {
                    hamsterProgress = new HamsterRewardProgressDto
                    {
                        SignUpCount = 0,
                        HamsterCount = 0,
                    };
                    if (referralList == null || referralList.Count == 0)
                    {
                        reward = 0 + " ELF";
                    }
                    else
                    {
                        reward = _hamsterOptions.ReferralReward + " ELF";
                    }
                }
                else
                {
                    var referralRecordList =
                        await _growthProvider.GetReferralRecordListAsync(null, caHolder.CaHash, 0, PagedResultRequestDto.MaxMaxResultCount,
                            Convert.ToDateTime(details.StartDate), Convert.ToDateTime(details.EndDate),
                            new List<int> { 1 });
                    hamsterProgress = new HamsterRewardProgressDto
                    {
                        SignUpCount = indexes.Count,
                        HamsterCount = referralRecordList.Count,
                    };

                    if (referralList == null || referralList.Count == 0)
                    {
                        reward = referralRecordList.Count * _hamsterOptions.HamsterReward + " ELF";
                    }
                    else
                    {
                        reward = referralRecordList.Count * _hamsterOptions.HamsterReward +
                                 _hamsterOptions.ReferralReward + " ELF";
                    }
                }

                var list = ModelToDictionary(hamsterProgress);
                data.Data = list;
                data.RewardProcessCount = reward;
                return data;
            }
            default:
                throw new UserFriendlyException("Invalidate Activity");
        }
    }

    public async Task<BeInvitedConfigResponseDto> GetBeInvitedConfigAsync()
    {
        var result = new BeInvitedConfigResponseDto();
        var data = new Dictionary<string, BeInvitedConfigDto>();
        var config = _beInvitedConfigOptions.BeInvitedConfig;
        foreach (var key in config.Keys)
        {
            var response = ObjectMapper.Map<BeInvitedConfig, BeInvitedConfigDto>(config[key]);
            response.TaskConfigs =
                ObjectMapper.Map<List<TaskConfigInfo>, List<TaskConfig>>(config[key].TaskConfigInfos);
            response.Notice = ObjectMapper.Map<NoticeInfo, Notice>(config[key].NoticeInfo);
            data.Add(key, response);
        }

        result.Data = data;
        result.ActivityTitle = _beInvitedConfigOptions.ActivityTitle;
        return result;
    }

    public async Task<ActivityBaseInfoDto> ActivityBaseInfoAsync()
    {
        var data = new List<ActivityBaseInfo>();
        var configs = _activityConfigOptions.ActivityConfigMap;
        foreach (var key in configs.Keys)
        {
            var baseInfo = new ActivityBaseInfo();
            var config = configs[key];
            baseInfo.ActivityName = key;
            baseInfo.StartDate = config.ActivityConfig.StartDate;
            baseInfo.EndDate = config.ActivityConfig.EndDate;
            baseInfo.IsDefault = config.IsDefault;
            var activityValue = (int)Enum.Parse(typeof(ActivityEnums), key);
            baseInfo.ActivityValue = activityValue;
            var sDate = DateTime.Parse(config.ActivityConfig.StartDate).ToString("MM.dd");
            var eDate = DateTime.Parse(config.ActivityConfig.EndDate).ToString("MM.dd");
            baseInfo.DateRange = sDate + "-" + eDate;
            data.Add(baseInfo);
        }

        return new ActivityBaseInfoDto
        {
            Data = data
        };
    }

    public async Task<ValidateHamsterScoreResponseDto> ValidateHamsterScoreAsync(string userId)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", userId);
        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var guardian = guardianGrain.GetGuardianAsync(userId).Result;
        if (!guardian.Message.IsNullOrEmpty())
        {
            return new ValidateHamsterScoreResponseDto
            {
                Result = new Result()
                {
                    ValidateResult = false
                },
                ErrorMsg = new ErrorMsg()
                {
                    Message = guardian.Message
                }
            };
        }

        var identifierHash = guardian.Data.IdentifierHash;
        var caHolderInfo =
            await _activityProvider.GetCaHolderInfoAsync(identifierHash);
        if (caHolderInfo == null || caHolderInfo.CaHolderInfo?.Count == 0)
        {
            return new ValidateHamsterScoreResponseDto()
            {
                Result = new Result()
                {
                    ValidateResult = false
                },
                ErrorMsg = new ErrorMsg()
                {
                    Message = "Account not exist."
                }
            };
        }

        var address = caHolderInfo.CaHolderInfo?.FirstOrDefault()?.CaAddress;
        var formatAddress = _hamsterOptions.AddressPrefix + address + _hamsterOptions.AddressSuffix;
        var hamsterScoreList =
            await _growthProvider.GetHamsterScoreListAsync(new List<string> { formatAddress },
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow);
        if (hamsterScoreList.GetScoreInfos?.Count == 0)
        {
            return new ValidateHamsterScoreResponseDto
            {
                Result = new Result()
                {
                    ValidateResult = false
                },
                ErrorMsg = new ErrorMsg()
                {
                    Message = "Validate failed."
                }
            };
        }

        return new ValidateHamsterScoreResponseDto
        {
            Result = new Result()
            {
                ValidateResult = true
            }
        };
    }

    private ActivityConfig GetActivityDetails(ActivityEnums activityEnum)
    {
        _activityConfigOptions.ActivityConfigMap.TryGetValue(activityEnum.ToString(), out var config);
        return config != null ? config.ActivityConfig : new ActivityConfig();
    }

    private async Task<List<ReferralRecordIndex>> GetHamsterSignUpCount(string caHash, string startDate, string endDate)
    {
        var growthInfo = await _growthProvider.GetReferralRecordListAsync(null, caHash, 0,
            PagedResultRequestDto.MaxMaxResultCount, Convert.ToDateTime(startDate), Convert.ToDateTime(endDate), new List<int> { 0 });
        return growthInfo;
    }

    private ActivityConfig GetActivityConfig(ActivityEnums activityEnum)
    {
        _activityConfigOptions.ActivityConfigMap.TryGetValue(activityEnum.ToString(), out var config);
        return config != null ? config.ActivityConfig : new ActivityConfig();
    }


    public async Task RepairHamsterDataAsync()
    {
        var repairList = await _growthProvider.GetInviteRepairIndexAsync();
        if (repairList == null || repairList.Count == 0)
        {
            _logger.LogDebug("No data need to be repaired.");
            return;
        }

        _logger.LogDebug("Total Count is {count}", repairList.Count);
        var count = 0;
        foreach (var repair in repairList)
        {
            try
            {
                var inviteCodes = repairList.Select(t => t.ReferralCode).ToList();
                //need to be added score
                var growthInfos = await _growthProvider.GetGrowthInfosAsync(null, inviteCodes);
                var growthInfoDic = growthInfos.Where(t => !t.InviteCode.IsNullOrEmpty())
                    .ToDictionary(t => t.InviteCode, t => t.CaHash);
                var caHolderInfo =
                    await _activityProvider.GetCaHolderInfoAsync(new List<string>(),
                        growthInfoDic[repair.ReferralCode]);
                var referralRecord = new ReferralRecordIndex
                {
                    CaHash = repair.CaHash,
                    ReferralCode = repair.ReferralCode,
                    IsDirectlyInvite = 0,
                    ReferralCaHash = growthInfoDic[repair.ReferralCode],
                    ReferralDate = repair.RegisterTime,
                    ReferralAddress = caHolderInfo.CaHolderInfo.FirstOrDefault()?.CaAddress
                };
                await _growthProvider.AddReferralRecordAsync(referralRecord);
                count++;
            }
            catch (Exception e)
            {
                _logger.LogDebug("Repair Data failed.Failed data is {data},reason is {msg}", repair.CaHash, e.Message);
            }
        }

        if (count == repairList.Count)
        {
            var expire = TimeSpan.FromDays(360);
            await _cacheProvider.Set(RepairDataCache, "Repair", expire);
            _logger.LogDebug("All data has been repaired.");
        }
    }

    public async Task CollectHamsterUserIdsAsync(string userId)
    {
        var expire = TimeSpan.FromDays(30);
        await _cacheProvider.SetAddAsync(HamsterTonGiftsUserIdsKey, userId, expire);
    }

    public async Task TonGiftsValidateAsync()
    {
        var userIds = await _cacheProvider.SetMembersAsync(HamsterTonGiftsUserIdsKey);
        if (userIds.Length == 0)
        {
            _logger.LogDebug("No users need to be validate.");
            return;
        }

        var ids = new List<string>();
        foreach (var id in userIds)
        {
            var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", id);
            var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
            var guardian = guardianGrain.GetGuardianAsync(id).Result;
            if (!guardian.Message.IsNullOrEmpty())
            {
                _logger.LogDebug("TonGift validate error : query user from grain error:{error}", guardian.Message);
                continue;
            }

            var identifierHash = guardian.Data.IdentifierHash;
            var caHolderInfo =
                await _activityProvider.GetCaHolderInfoAsync(identifierHash);
            if (caHolderInfo == null || caHolderInfo.CaHolderInfo.Count == 0)
            {
                _logger.LogDebug("TonGift validate error : query user from graphQl error: user not exists");
                continue;
            }

            ids.Add(id);
        }

        var param = new TonGiftsRequestDto()
        {
            TaskId = _tonGiftsOptions.TaskId,
            Status = "completed",
            UserIds = ids
        };
        var rawStr = JsonConvert.SerializeObject(param);
        var t = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
        var Hash = HMACSHA256Helper.ComputeHash("rawStr=" + param + "&t=" + t, _tonGiftsOptions.ApiKey);
        var apiKey = _tonGiftsOptions.ApiKey;
        const string url = "https://devmini.tongifts.app/";
        var client = _httpClientFactory.CreateClient();
        var tokenParam = JsonConvert.SerializeObject(new
            { rawStr, apiKey, Hash, t });
        var requestParam = new StringContent(tokenParam,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var response = await client.PostAsync(url, requestParam);
        var result = await response.Content.ReadAsStringAsync();
    }

    public async Task<GetGrowthInfosDto> GetGrowthInfosAsync(GetGrowthInfosRequestDto input)
    {
        if (input.ReferralCodes.IsNullOrEmpty() && input.ProjectCode.IsNullOrEmpty())
        {
            throw new AbpValidationException("referralCodes and projectCode is empty.");
        }
        var result = await _growthProvider.GetGrowthInfosAsync(input);
        return new GetGrowthInfosDto()
        {
            TotalRecordCount = result.Item1,
            Data = ObjectMapper.Map<List<GrowthIndex>, List<GrowthUserInfoDto>>(result.Item2)
        };
    }

    
    public async Task TonGiftsValidateAsync()
    {
        if (!_tonGiftsOptions.IsStart)
        {
            _logger.LogInformation("TonGiftsValidateAsync not starting");
        }

        // get transaction list
        var (fromAddressSet, nextBlockHeight) = await getFromAddressSet();
        if (fromAddressSet?.Count == 0)
        {
            _logger.LogInformation("TonGiftsValidateAsync fromAddressSet = 0");
            return;
        }
        _logger.LogInformation("TonGiftsValidateAsync fromAddressSet = {0} nextBlockHeight = {1}", JsonSerializer.Serialize(fromAddressSet), JsonSerializer.Serialize(fromAddressSet));

        // get identifierHash list
        HashSet<string> identifierHashSet = await getIdentifierHashSet(fromAddressSet);
        if (identifierHashSet?.Count == 0)
        {
            _logger.LogInformation("TonGiftsValidateAsync getIdentifierHashSet = 0");
            return;
        }
        _logger.LogInformation("TonGiftsValidateAsync getIdentifierHashSet = {0}", JsonSerializer.Serialize(identifierHashSet));

        // get identifier list
        HashSet<string> telegramIdSet = await getTelegramIdSet(identifierHashSet);
        if (telegramIdSet?.Count == 0)
        {
            _logger.LogInformation("TonGiftsValidateAsync telegramIdSet = 0");
            return;
        }
        _logger.LogInformation("TonGiftsValidateAsync telegramIdSet = {0}",JsonSerializer.Serialize(telegramIdSet));
        
        // to send, it should not be block process, so add try-catch
        try
        {
            await TonGiftsToCall(telegramIdSet);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TonGiftsValidateAsync TonGiftsToCall has error {0}",e.Message);
        }

        // update blockHeight userId 
        await _cacheProvider.Set(HamsterTonGiftsConstant.HeightKey, nextBlockHeight.ToString(), HamsterTonGiftsConstant.KeyExpire);
    }

    private async Task<HashSet<string>> getTelegramIdSet(HashSet<string> identifierHashSet)
    {
        HashSet<string> telegramIdSet = new HashSet<string>();

        List<GuardianIndexDto> guardianIndexDtos = await _guardianAppService.GetGuardianListAsync(identifierHashSet.ToList());
        if (guardianIndexDtos?.Count == 0)
        {
            return telegramIdSet;
        }

        guardianIndexDtos.ForEach(t => telegramIdSet.Add(t.Identifier));
        return telegramIdSet;
    }

    private async Task<HashSet<string>> getIdentifierHashSet(HashSet<string> fromAddressSet)
    {
        HashSet<string> identifierHashSet = new HashSet<string>();
        GuardiansDto guardiansDto = await _activityProvider.GetCaHolderInfoAsync(fromAddressSet.ToList(), null, 0, HamsterTonGiftsConstant.MaxResultCount);
        if (guardiansDto?.CaHolderInfo?.Count == 0)
        {
            return identifierHashSet;
        }

        guardiansDto.CaHolderInfo
            .Where(info => info.GuardianList?.Guardians?.Any() == true)
            .SelectMany(info => info.GuardianList.Guardians)
            .Select(guardian => guardian.IdentifierHash)
            .ForEach(identifierHash => identifierHashSet.Add(identifierHash));

        return identifierHashSet;
    }

    private async Task<(HashSet<string>, long)> getFromAddressSet()
    {
        HashSet<string> fromAddressSet = new HashSet<string>();
        long nextBlockHeight = 0;

        string startBlockHeight = await _cacheProvider.Get(HamsterTonGiftsConstant.HeightKey);
        long endBlockHeight = await _graphQlProvider.GetIndexBlockHeightAsync(_tonGiftsOptions.ChainId);
        if (null == startBlockHeight)
        {
            await _cacheProvider.Set(HamsterTonGiftsConstant.HeightKey, endBlockHeight.ToString(), HamsterTonGiftsConstant.KeyExpire);
            nextBlockHeight = endBlockHeight;
            return (fromAddressSet, nextBlockHeight);
        }

        IndexerTransactions indexerTransactions = await _activityProvider.GetActivitiesAsync(_tonGiftsOptions.ChainId, new List<string> { "Play" }, long.Parse
                (startBlockHeight), endBlockHeight,
            HamsterTonGiftsConstant.MaxResultCount);
        if (indexerTransactions?.CaHolderTransaction?.Data?.Count == 0)
        {
            nextBlockHeight = long.Parse(startBlockHeight);
            return (fromAddressSet, nextBlockHeight);
        }

        fromAddressSet = indexerTransactions.CaHolderTransaction.Data
            .Where(t => t.ToContractAddress == _tonGiftsOptions.ToContractAddress)
            .Select(t => t.FromAddress)
            .ToHashSet();
        nextBlockHeight = indexerTransactions.CaHolderTransaction.Data.Max(t => t.BlockHeight);
        return (fromAddressSet, nextBlockHeight);
    }

    public async Task TonGiftsToCall(HashSet<string> telegramIdSet)
    {
        // insert
        var doneList = await _cacheProvider.SetMembersAsync(HamsterTonGiftsConstant.DoneUserIdsKeyPrefix + _tonGiftsOptions.TaskId);
        var toAddList = telegramIdSet.Where(t => !doneList.Contains(t)).ToList();
        await _cacheProvider.SetAddAsync(HamsterTonGiftsConstant.UserIdsKey, toAddList, HamsterTonGiftsConstant.KeyExpire);

        var userIds = await _cacheProvider.SetMembersAsync(HamsterTonGiftsConstant.UserIdsKey);
        if (userIds?.Length == 0)
        {
            _logger.LogInformation("TonGiftsValidateAsync TonGiftsToCall No users need to be validate.");
            return;
        }

        // call
        var ids = userIds.Select(t => t.ToString()).ToList();
        var param = new TonGiftsRequestDto()
        {
            Status = HamsterTonGiftsConstant.StatusCompleted,
            UserIds = ids,
            TaskId = _tonGiftsOptions.TaskId,
            K = _tonGiftsOptions.Id,
            T = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()
        };
        param.S = HMACSHA256Helper.GenerateSignature(param, _tonGiftsOptions.ApiKey);

        var client = _httpClientFactory.CreateClient();
        var tokenParam = JsonConvert.SerializeObject(param);
        var requestParam = new StringContent(tokenParam, Encoding.UTF8, MediaTypeNames.Application.Json);
        var response = await client.PostAsync(_tonGiftsOptions.HostUrl, requestParam);
        var result = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("TonGiftsValidateAsync TonGiftsToCall client response: {0}",result);
        
        // handle reusult
        await handleTonGiftsResult(result, ids);
    }

    private async Task handleTonGiftsResult(string result, List<string> allIDs)
    {
        TonGiftsResponseDto responseDto = JsonConvert.DeserializeObject<TonGiftsResponseDto>(result);
        List<string> successfulUpdates = responseDto.Message.Contains("successfully") ? allIDs : responseDto.SuccessfulUpdates.Select(p => p.UserId).ToList();
        _logger.LogInformation("TonGiftsValidateAsync handleTonGiftsResult successfulUpdates = {0}", JsonSerializer.Serialize(successfulUpdates));

        await _cacheProvider.SetAddAsync(HamsterTonGiftsConstant.DoneUserIdsKeyPrefix + _tonGiftsOptions.TaskId, successfulUpdates,
            HamsterTonGiftsConstant.KeyExpire);
        await _cacheProvider.SetRemoveAsync(HamsterTonGiftsConstant.UserIdsKey, successfulUpdates);
    }
    
    private async Task<Dictionary<string, CAHolderIndex>> GetNickNameByCaHashes(List<string> caHashes)
    {
        var caHolderList = await GetCaHolderByCaHashAsync(caHashes);
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

    private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dtDateTime;
    }

    private long StringToTimeStamp(string dateString)
    {
        var dateTime = DateTime.Parse(dateString);
        return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
    }

    private List<ReferralCountDto> ModelToDictionary(object obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        var modelToDic = obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(
                prop => prop.Name,
                prop => prop.GetValue(obj, null)
            );


        return modelToDic.Keys.Select(model => new ReferralCountDto()
            {
                ActivityName = _hamsterOptions.HamsterCopyWriting[model], ReferralCount = modelToDic[model].ToString()
            })
            .ToList();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.Notify;
using CAServer.IpInfo;
using CAServer.Notify.Dtos;
using CAServer.Notify.Etos;
using CAServer.Notify.Provider;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.Notify;

[RemoteService(false), DisableAuditing]
public class NotifyAppService : CAServerAppService, INotifyAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INESTRepository<NotifyRulesIndex, Guid> _notifyRulesRepository;
    private readonly INotifyProvider _notifyProvider;
    private readonly IIpInfoAppService _ipInfoAppService;
    private readonly IDistributedCache<VersionInfo> _distributedCache;

    public NotifyAppService(IDistributedEventBus distributedEventBus,
        IClusterClient clusterClient,
        INESTRepository<NotifyRulesIndex, Guid> notifyRulesRepository,
        INotifyProvider notifyProvider,
        IIpInfoAppService ipInfoAppService, IDistributedCache<VersionInfo> distributedCache)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _notifyRulesRepository = notifyRulesRepository;
        _notifyProvider = notifyProvider;
        _ipInfoAppService = ipInfoAppService;
        _distributedCache = distributedCache;
    }

    public async Task<int> FireAsync(string token, string title, string content)
    {
        var message = new MulticastMessage()
        {
            Notification = new Notification()
            {
                Title = "Hello from C# Firebase Admin SDK!",
                Body = "This is a test notification"
            },
            Tokens = new List<string>()
            {
                "f_rJmfIjWkRzuKdmcya4b7:APA91bHadg2GNLDV3NcpwG-hDVq1tXjEqBRiT8rrQq7_ANgQjEGadSYTgelgx20KS3BUhxp8lX1qJwy2R51-VFTrjOXEyB5zDzYh0i0Cah9t5cdoNqDNLHGpYxqlEGn83msHjJuzGmM-",
                "di0iTU9JTpGwuW9c72aoCM:APA91bFYewRh5wfWumKfzndhk3XA8yiJK-t_pI8VZS8eRuFv8VGJbJ19Gk2F01BX-oZ7HrlZVGCXM73l9uLD0wqxccuRim5MjOPM3r7ofDrq1p-w_bmyLq9YCsaPWv4sg03N29dyH3UM",
                "fBca4zRmTw-0pKfvcXTLYk:APA91bEUAm_81kaB0iY4hT_E361Lu085Cjoa36Yhh6m-PMvvrej7Hawyk_bpEtVqu7RL1xxNk1ALjhvRr32fEEtRL56Rs5UFBm61zv-SzJ8ntaPhAkYsLdVay0tHgIgQvBqNTzWU4iED"
            },
            Android = new AndroidConfig()
            {
                Notification = new AndroidNotification()
                {
                    Title = "Hello from C# Firebase Admin SDK!",
                    Body = "This is a test notification",
                    Icon = "https://profile-avatar.csdnimg.cn/91e6e8ae5e7d45b9a71e7147219f82af_baidu_38845827.jpg!1",
                    NotificationCount = 0,
                }
            },
            Apns = new ApnsConfig()
            {
                CustomData = new Dictionary<string, object>() { ["badge"] = 100000000 ,["aps"]=new Aps()
                {
                    Badge = 100000000
                }}
            }
        };

        Console.WriteLine(JsonConvert.SerializeObject(message.Android));
        var messaging = FirebaseMessaging.DefaultInstance;
        var result = await messaging.SendMulticastAsync(message);
        return result.SuccessCount;
    }

    public async Task<PullNotifyResultDto> PullNotifyAsync(PullNotifyDto input)
    {
        var rules = await GetNotifyAsync(input);
        if (rules.IsNullOrEmpty()) return null;

        var notifyRules = rules.First();
        if (notifyRules.Countries is { Length: > 0 })
        {
            var ipInfo = await _ipInfoAppService.GetIpInfoAsync();
            notifyRules = rules?.Where(t => t.Countries.Contains(ipInfo.Code)).FirstOrDefault();
            if (notifyRules == null)
            {
                return null;
            }
        }

        var grain = _clusterClient.GetGrain<INotifyGrain>(notifyRules.Id);
        var resultDto = await grain.GetNotifyAsync();
        if (!resultDto.Success)
        {
            throw new UserFriendlyException(resultDto.Message);
        }

        var notifyDto = ObjectMapper.Map<NotifyGrainDto, PullNotifyResultDto>(resultDto.Data);
        if (!notifyDto.IsForceUpdate)
        {
            notifyDto.IsForceUpdate = await GetForceUpdateAsync(rules, input.AppVersion, input.DeviceType.ToString());
        }

        return notifyDto;
    }

    private async Task<List<NotifyRulesIndex>> GetNotifyAsync(PullNotifyDto input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NotifyRulesIndex>, QueryContainer>>();
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.AppId).Value(input.AppId)));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.DeviceTypes).Terms(input.DeviceType.ToString())));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.AppVersions).Terms(input.AppVersion)));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.DeviceBrands).Terms(input.DeviceBrand)));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.OperatingSystemVersions).Terms(input.OperatingSystemVersion)));

        QueryContainer Filter(QueryContainerDescriptor<NotifyRulesIndex> f) => f.Bool(b => b.Must(mustQuery));
        IPromise<IList<ISort>> Sort(SortDescriptor<NotifyRulesIndex> s) => s.Descending(t => t.NotifyId);

        var (totalCount, notifyRulesIndices) = await _notifyRulesRepository.GetSortListAsync(Filter, sortFunc: Sort);

        if (totalCount <= 0)
        {
            return new List<NotifyRulesIndex>();
        }

        return notifyRulesIndices;
    }

    public async Task<NotifyResultDto> CreateAsync(CreateNotifyDto notifyDto)
    {
        var grainId = GuidGenerator.Create();
        var grain = _clusterClient.GetGrain<INotifyGrain>(grainId);

        var resultDto =
            await grain.AddNotifyAsync(
                ObjectMapper.Map<CreateNotifyDto, NotifyGrainDto>(notifyDto));

        if (!resultDto.Success)
        {
            throw new UserFriendlyException(resultDto.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<NotifyGrainDto, NotifyEto>(resultDto.Data));
        return ObjectMapper.Map<NotifyGrainDto, NotifyResultDto>(resultDto.Data);
    }

    public async Task<NotifyResultDto> UpdateAsync(Guid id, UpdateNotifyDto notifyDto)
    {
        var grain = _clusterClient.GetGrain<INotifyGrain>(id);

        var resultDto =
            await grain.UpdateNotifyAsync(
                ObjectMapper.Map<UpdateNotifyDto, NotifyGrainDto>(notifyDto));

        if (!resultDto.Success)
        {
            throw new UserFriendlyException(resultDto.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<NotifyGrainDto, NotifyEto>(resultDto.Data));
        return ObjectMapper.Map<NotifyGrainDto, NotifyResultDto>(resultDto.Data);
    }

    public async Task DeleteAsync(Guid id)
    {
        var grain = _clusterClient.GetGrain<INotifyGrain>(id);
        var resultDto = await grain.DeleteNotifyAsync(id);

        if (!resultDto.Success)
        {
            throw new UserFriendlyException(resultDto.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<NotifyGrainDto, DeleteNotifyEto>(resultDto.Data));
    }

    public async Task<List<NotifyResultDto>> CreateFromCmsAsync(string version)
    {
        //get data from cms
        var condition =
            "/items/upgradePush?fields=*,countries.country_id.value,deviceBrands.deviceBrand_id.value,deviceTypes.deviceType_id.value,targetVersion.value," +
            $"appVersions.appVersion_id.value,styleType.value,targetVersion.value&filter[targetVersion][value][_eq]={version}&filter[status][_eq]=published";

        Logger.LogDebug("before get data from cms.");
        var notifyDto = await GetDataAsync(condition);

        var result = new List<NotifyResultDto>();

        foreach (var notify in notifyDto.Data)
        {
            var grainId = GuidGenerator.Create();
            var grain = _clusterClient.GetGrain<INotifyGrain>(grainId);

            var resultDto = await grain.AddNotifyAsync(ObjectMapper.Map<CmsNotify, NotifyGrainDto>(notify));
            if (!resultDto.Success)
            {
                throw new UserFriendlyException(resultDto.Message);
            }

            await _distributedEventBus.PublishAsync(ObjectMapper.Map<NotifyGrainDto, NotifyEto>(resultDto.Data), false,
                false);
            result.Add(ObjectMapper.Map<NotifyGrainDto, NotifyResultDto>(resultDto.Data));
        }

        return result;
    }

    public async Task<NotifyResultDto> UpdateFromCmsAsync(Guid id)
    {
        var grain = _clusterClient.GetGrain<INotifyGrain>(id);
        var getResultDto = await grain.GetNotifyAsync();
        if (!getResultDto.Success)
        {
            throw new UserFriendlyException(getResultDto.Message);
        }

        var condition =
            "/items/upgradePush?fields=*,countries.country_id.value,deviceBrands.deviceBrand_id.value,deviceTypes.deviceType_id.value," +
            $"targetVersion.value,appVersions.appVersion_id.value,styleType.value,targetVersion.value&filter[id][_eq]={getResultDto.Data.NotifyId}";
        var notifyDto = await GetDataAsync(condition);

        var cmsNotify = notifyDto.Data.First();
        var resultDto = await grain.UpdateNotifyAsync(ObjectMapper.Map<CmsNotify, NotifyGrainDto>(cmsNotify));

        if (!resultDto.Success)
        {
            throw new UserFriendlyException(resultDto.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<NotifyGrainDto, NotifyEto>(resultDto.Data), false,
            false);
        return ObjectMapper.Map<NotifyGrainDto, NotifyResultDto>(resultDto.Data);
    }

    private async Task<CmsNotifyDto> GetDataAsync(string condition)
    {
        var notifyDto = await _notifyProvider.GetDataFromCms<CmsNotifyDto>(condition);

        if (notifyDto?.Data == null || notifyDto.Data.Count == 0)
        {
            throw new UserFriendlyException($"Get data from cms fail: {condition}");
        }

        Logger.LogDebug("Get data from cms: {data}", JsonConvert.SerializeObject(notifyDto));

        return notifyDto;
    }

    private async Task<bool> GetForceUpdateAsync(List<NotifyRulesIndex> rules, string version, string deviceTypes)
    {
        var key = $"{CommonConstant.AppVersionKeyPrefix}:{version}:{deviceTypes}";
        var versionInfo = await _distributedCache.GetAsync(key);
        if (versionInfo != null)
        {
            return versionInfo.IsForceUpdate;
        }

        var isForceUpdate = await GetForceUpdateAsync(rules);
        await _distributedCache.SetAsync(key, new VersionInfo { Version = version, IsForceUpdate = isForceUpdate },
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = CommonConstant.DefaultAbsoluteExpiration
            });

        return isForceUpdate;
    }

    private async Task<bool> GetForceUpdateAsync(List<NotifyRulesIndex> rules)
    {
        rules.Remove(rules.First());
        foreach (var rule in rules)
        {
            var grain = _clusterClient.GetGrain<INotifyGrain>(rule.Id);

            var resultDto = await grain.GetNotifyAsync();
            if (!resultDto.Success)
            {
                Logger.LogError("get notify error, notifyId: {notifyId}", rule.Id);
                continue;
            }

            if (resultDto.Data.IsForceUpdate)
            {
                return true;
            }
        }

        return false;
    }
}
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.Grains;
using CAServer.Grains.Grain.Growth;
using CAServer.Growth.Dtos;
using CAServer.Growth.Etos;
using CAServer.RedDot;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.Growth;

[RemoteService(false), DisableAuditing]
public class GrowthAppService : CAServerAppService, IGrowthAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INESTRepository<GrowthIndex, string> _growthRepository;
    private readonly IRedDotAppService _redDotAppService;

    public GrowthAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        INESTRepository<GrowthIndex, string> growthRepository, IRedDotAppService redDotAppService)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _growthRepository = growthRepository;
        _redDotAppService = redDotAppService;
    }

    public async Task<GrowthRedDotDto> GetRedDotAsync()
    {
        var redDotInfo = await _redDotAppService.GetRedDotInfoAsync(RedDotType.Referral);
        var status = redDotInfo?.Status == RedDotStatus.Read ? RedDotStatus.Read : RedDotStatus.Unread;
        return new GrowthRedDotDto()
        {
            Status = status
        };
    }

    public async Task SetRedDotAsync()
    {
        await _redDotAppService.SetRedDotAsync(RedDotType.Referral);
    }

    public async Task<ShortLinkDto> GetShortLinkAsync(string projectCode)
    {
        var grainId = GrainIdHelper.GenerateGrainId(CommonConstant.UserGrowthPrefix, CurrentUser.GetId());
        var growthGrain = _clusterClient.GetGrain<IGrowthGrain>(grainId);
        var result = await growthGrain.GetGrowthInfo();

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        var url = $"xxx/{result.Data.ShortLinkCode}";
        return new ShortLinkDto()
        {
            ShortLink = url
        };
    }

    public async Task CreateGrowthInfoAsync()
    {
        var userId = CurrentUser.GetId();
        var grainId = GrainIdHelper.GenerateGrainId(CommonConstant.UserGrowthPrefix, userId);
        var growthGrain = _clusterClient.GetGrain<IGrowthGrain>(grainId);
        var result = await growthGrain.CreateGrowthInfo(new GrowthGrainDto()
        {
            UserId = userId
        });

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<GrowthGrainDto, CreateGrowthEto>(result.Data), false,
            false);
    }
}
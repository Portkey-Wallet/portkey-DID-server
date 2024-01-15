using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.Grains;
using CAServer.Grains.Grain.RedDot;
using CAServer.RedDot.Dtos;
using CAServer.RedDot.Etos;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;
using RedDotInfo = CAServer.Entities.Es.RedDotInfo;

namespace CAServer.RedDot;

[RemoteService(false), DisableAuditing]
public class RedDotAppService : CAServerAppService, IRedDotAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INESTRepository<RedDotIndex, string> _redDotRepository;

    public RedDotAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        INESTRepository<RedDotIndex, string> redDotRepository)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _redDotRepository = redDotRepository;
    }

    public async Task<RedDotInfoDto> GetRedDotInfoAsync(RedDotType redDotType)
    {
        var redDotId = GrainIdHelper.GenerateGrainId(CommonConstant.RedDotPrefix, CurrentUser.GetId());
        var redDot = await _redDotRepository.GetAsync(redDotId);

        var redDotInfo = redDot?.RedDotInfos?.FirstOrDefault(t => t.RedDotType == redDotType.ToString());
        return ObjectMapper.Map<RedDotInfo, RedDotInfoDto>(redDotInfo);
    }

    public async Task SetRedDotAsync(RedDotType redDotType)
    {
        var userId = CurrentUser.GetId();
        var grainId = GrainIdHelper.GenerateGrainId(CommonConstant.RedDotPrefix, userId);
        var growthGrain = _clusterClient.GetGrain<IRedDotGrain>(grainId);
        var result = await growthGrain.SetRedDot(redDotType);

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        var redDotEto = ObjectMapper.Map<RedDotGrainDto, RedDotEto>(result.Data);
        redDotEto.UserId = userId;
        await _distributedEventBus.PublishAsync(redDotEto, false, false);
    }
}
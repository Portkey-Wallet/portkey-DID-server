using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain.Upgrade;
using CAServer.Options;
using CAServer.Upgrade.Dtos;
using CAServer.Upgrade.Etos;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.Upgrade;

[RemoteService(false), DisableAuditing]
public class UpgradeAppService : CAServerAppService, IUpgradeAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INESTRepository<UpgradeInfoIndex, string> _upgradeInfoRepository;
    private readonly SwitchOptions _switchOptions;

    public UpgradeAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        INESTRepository<UpgradeInfoIndex, string> upgradeInfoRepository,
        IOptionsSnapshot<SwitchOptions> switchOptions)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _upgradeInfoRepository = upgradeInfoRepository;
        _switchOptions = switchOptions.Value;
    }

    public async Task<UpgradeResponseDto> GetUpgradeInfoAsync(UpgradeRequestDto input)
    {
        if (!_switchOptions.UpgradePopup)
        {
            return new UpgradeResponseDto()
            {
                IsPopup = true
            };
        }

        var grainId = GrainIdHelper.GenerateGrainId(CommonConstant.UpgradeGrainIdPrefix, CurrentUser.GetId());
        var upgradeInfo = await _upgradeInfoRepository.GetAsync(grainId);
        if (upgradeInfo?.Version == input.Version.ToUpper())
        {
            return ObjectMapper.Map<UpgradeInfoIndex, UpgradeResponseDto>(upgradeInfo);
        }

        return new UpgradeResponseDto();
    }

    public async Task CloseAsync(UpgradeRequestDto input)
    {
        if (!_switchOptions.UpgradePopup) return;

        var userId = CurrentUser.GetId();
        var grainId = GrainIdHelper.GenerateGrainId(CommonConstant.UpgradeGrainIdPrefix, userId);
        var upgradeGrain = _clusterClient.GetGrain<IUpgradeGrain>(grainId);
        var result = await upgradeGrain.AddUpgradeInfo(new UpgradeGrainDto()
        {
            Version = input.Version.ToUpper(),
            UserId = userId
        });

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<UpgradeGrainDto, CreateUpgradeInfoEto>(result.Data));
    }
}
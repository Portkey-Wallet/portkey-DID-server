using CAServer.Grains.State.Upgrade;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Upgrade;

public class UpgradeGrain : Grain<UpgradeState>, IUpgradeGrain
{
    private readonly IObjectMapper _objectMapper;

    public UpgradeGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, token);
    }

    public async Task<GrainResultDto<UpgradeGrainDto>> AddUpgradeInfo(UpgradeGrainDto upgradeDto)
    {
        var result = new GrainResultDto<UpgradeGrainDto>();
        State = _objectMapper.Map<UpgradeGrainDto, UpgradeState>(upgradeDto);
        State.Id = this.GetPrimaryKeyString();
        State.IsPopup = true;
        State.CreateTime = DateTime.UtcNow;
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<UpgradeState, UpgradeGrainDto>(State);
        return result;
    }
}
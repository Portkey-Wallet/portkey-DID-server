namespace CAServer.Grains.Grain.Upgrade;

public interface IUpgradeGrain: IGrainWithStringKey
{
    Task<GrainResultDto<UpgradeGrainDto>> AddUpgradeInfo(UpgradeGrainDto upgradeDto);
}
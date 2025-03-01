namespace CAServer.Grains.Grain.Growth;

public interface IGrowthGrain : IGrainWithStringKey
{
    Task<GrainResultDto<GrowthGrainDto>> CreateGrowthInfo(GrowthGrainDto growthGrainDto);
    Task<GrainResultDto<GrowthGrainDto>> GetGrowthInfo(string projectCode);
    Task<bool> Exist(string projectCode);
}
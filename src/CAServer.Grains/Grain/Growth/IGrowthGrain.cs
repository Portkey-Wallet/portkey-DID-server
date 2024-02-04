using Orleans;

namespace CAServer.Grains.Grain.Growth;

public interface IGrowthGrain : IGrainWithStringKey
{
    Task<GrainResultDto<GrowthGrainDto>> CreateGrowthInfo(GrowthGrainDto growthGrainDto);
    Task<GrainResultDto<GrowthGrainDto>> GetGrowthInfo();
    Task<bool> Exist();
}
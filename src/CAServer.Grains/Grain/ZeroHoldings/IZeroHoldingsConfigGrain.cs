
using Orleans;

namespace CAServer.Grains.Grain.ZeroHoldings;

public interface IZeroHoldingsConfigGrain: IGrainWithGuidKey
{
    Task<bool> AddOrUpdateAsync(ZeroHoldingsGrainDto userExtraInfoGrainDto);
    
    Task<GrainResultDto<ZeroHoldingsGrainDto>> GetAsync(Guid userId);
}
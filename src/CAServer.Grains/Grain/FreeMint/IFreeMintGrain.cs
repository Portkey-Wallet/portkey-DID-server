using CAServer.FreeMint.Dtos;
using Orleans;

namespace CAServer.Grains.Grain.FreeMint;

public interface IFreeMintGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<FreeMintGrainDto>> GetFreeMintInfo();
    Task<GrainResultDto<GetRecentStatusDto>> GetRecentStatus();
    Task<GrainResultDto<GetRecentStatusDto>> Mint();
}
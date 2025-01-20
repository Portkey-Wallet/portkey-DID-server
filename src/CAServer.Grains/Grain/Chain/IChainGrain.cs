using CAServer.Grains.Grain.Account;

namespace CAServer.Grains.Grain.Chain;

public interface IChainGrain : IGrainWithStringKey
{
    Task<GrainResultDto<ChainGrainDto>> AddChainAsync(ChainGrainDto chainGrainDto);
    Task<GrainResultDto<ChainGrainDto>> UpdateChainAsync(ChainGrainDto chainGrainDto);
    Task<GrainResultDto<ChainGrainDto>> DeleteChainAsync();
}
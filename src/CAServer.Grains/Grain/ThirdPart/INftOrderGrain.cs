using CAServer.ThirdPart;

namespace CAServer.Grains.Grain.ThirdPart;

public interface INftOrderGrain : IGrainWithGuidKey
{

    Task<GrainResultDto<NftOrderGrainDto>> CreateNftOrder(NftOrderGrainDto input);

    Task<GrainResultDto<NftOrderGrainDto>> UpdateNftOrder(NftOrderGrainDto input);

    Task<GrainResultDto<NftOrderGrainDto>> GetNftOrder();

}
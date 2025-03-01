using CAServer.EnumType;
using CAServer.FreeMint.Dtos;

namespace CAServer.Grains.Grain.FreeMint;

public interface IFreeMintGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<FreeMintGrainDto>> GetFreeMintInfo();
    Task<GrainResultDto<GetRecentStatusDto>> GetRecentStatus();
    Task<GrainResultDto<GetRecentStatusDto>> GetMintStatus(string itemId);

    Task<GrainResultDto<ItemMintInfo>> SaveMintInfo(MintNftDto mintNftDto);
    Task<GrainResultDto<ItemMintInfo>> ChangeMintStatus(string itemId, FreeMintStatus status);
    Task<bool> CheckLimitExceed();
}
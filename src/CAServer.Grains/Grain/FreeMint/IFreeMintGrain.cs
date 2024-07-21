using CAServer.EnumType;
using CAServer.FreeMint.Dtos;
using Orleans;

namespace CAServer.Grains.Grain.FreeMint;

public interface IFreeMintGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<FreeMintGrainDto>> GetFreeMintInfo();
    Task<GrainResultDto<GetRecentStatusDto>> GetRecentStatus();
    Task<GrainResultDto<GetRecentStatusDto>> GetMintStatus(string itemId);

    Task<GrainResultDto<ItemMintInfo>> SaveMintInfo(MintNftDto mintNftDto);
    Task<GrainResultDto<string>> GetTokenId();

    Task<GrainResultDto<ItemMintInfo>> ChangeMintStatus(string itemId, FreeMintStatus status);
}
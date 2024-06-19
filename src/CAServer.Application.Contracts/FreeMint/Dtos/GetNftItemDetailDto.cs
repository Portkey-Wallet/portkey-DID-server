using CAServer.EnumType;
using CAServer.UserAssets.Dtos;

namespace CAServer.FreeMint.Dtos;

public class GetNftItemDetailDto : NftItem
{
    public FreeMintStatus Status { get; set; }
}
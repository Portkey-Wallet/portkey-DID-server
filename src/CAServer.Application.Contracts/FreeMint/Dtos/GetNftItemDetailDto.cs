using CAServer.Commons.Etos;
using CAServer.EnumType;
using CAServer.UserAssets.Dtos;

namespace CAServer.FreeMint.Dtos;

public class GetNftItemDetailDto : ChainDisplayNameDto
{
    public string Symbol { get; set; }
    public string TokenId { get; set; }
    public string Alias { get; set; }
    public string Balance { get; set; }
    public long TotalSupply { get; set; }
    public long CirculatingSupply { get; set; }
    public string ImageUrl { get; set; }
    public string TokenContractAddress { get; set; }
    public string ImageLargeUrl { get; set; }
    public string Decimals { get; set; }
    public string CollectionSymbol { get; set; }
    public string TokenName { get; set; }
    public FreeMintStatus Status { get; set; }
}
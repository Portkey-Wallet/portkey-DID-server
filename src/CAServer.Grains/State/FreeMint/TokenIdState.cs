using CAServer.Commons;

namespace CAServer.Grains.State.FreeMint;

[GenerateSerializer]
public class TokenIdState
{
	[Id(0)]
    public int CurrentTokenId { get; set; } = CommonConstant.InitTokenId;

	[Id(1)]
    public List<int> UsedTokenIds { get; set; } = new List<int>();
}

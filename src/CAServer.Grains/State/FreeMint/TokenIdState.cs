using CAServer.Commons;

namespace CAServer.Grains.State.FreeMint;

public class TokenIdState
{
    public int CurrentTokenId { get; set; } = CommonConstant.InitInviteCode;

    public List<int> UsedTokenIds { get; set; } = new List<int>();
}
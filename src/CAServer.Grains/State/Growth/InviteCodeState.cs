using CAServer.Commons;

namespace CAServer.Grains.State.Growth;

public class InviteCodeState
{
    public int CurrentInviteCode { get; set; } = CommonConstant.InitInviteCode;
}
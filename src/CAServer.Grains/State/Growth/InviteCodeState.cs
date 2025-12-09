using CAServer.Commons;

namespace CAServer.Grains.State.Growth;

[GenerateSerializer]
public class InviteCodeState
{
	[Id(0)]
    public int CurrentInviteCode { get; set; } = CommonConstant.InitInviteCode;
}

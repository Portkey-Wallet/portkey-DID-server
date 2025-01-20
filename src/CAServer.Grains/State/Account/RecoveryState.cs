using CAServer.Account;
using CAServer.CAAccount.Dtos;

namespace CAServer.Grains.State;

[GenerateSerializer]
public class RecoveryState
{
	[Id(0)]
    public string Id { get; set; }
	[Id(1)]
    public List<RecoveryInfo> RecoveryInfo { get; set; } = new();
}

[GenerateSerializer]
public class RecoveryInfo : CAAccountBase
{
	[Id(0)]
    public string GrainId { get; set; }
	[Id(1)]
    public List<GuardianInfo> GuardianApproved { get; set; }
	[Id(2)]
    public string LoginGuardianIdentifierHash { get; set; }
	[Id(3)]
    public DateTime? RecoveryTime { get; set; }
	[Id(4)]
    public bool? RecoverySuccess { get; set; }
	[Id(5)]
    public string RecoveryMessage { get; set; }
	[Id(6)]
    public ReferralInfo ReferralInfo { get; set; }
}

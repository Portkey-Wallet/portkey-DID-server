using CAServer.Account;

namespace CAServer.Grains.State;

public class RecoveryState
{
    public string Id { get; set; }
    public List<RecoveryInfo> RecoveryInfo { get; set; } = new();
}

public class RecoveryInfo : CAAccountBase
{
    public string GrainId { get; set; }
    public List<GuardianInfo> GuardianApproved { get; set; }
    public string LoginGuardianIdentifierHash { get; set; }
    public DateTime? RecoveryTime { get; set; }
    public bool? RecoverySuccess { get; set; }
    public string RecoveryMessage { get; set; }
}
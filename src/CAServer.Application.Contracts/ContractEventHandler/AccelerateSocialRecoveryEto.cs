using System;
using CAServer.Account;
using CAServer.Hubs;

namespace CAServer.ContractEventHandler;

public class AccelerateSocialRecoveryEto
{
    public AccelerateSocialRecoveryEto()
    {
        RecoveryTime = DateTime.Now;
    }

    public Guid Id { get; set; }

    public string CaHash { get; set; }
    public string CaAddress { get; set; }

    public string IdentifierHash { get; set; }

    public string GrainId { get; set; }
    public DateTime RecoveryTime { get; set; }
    public string RecoveryMessage { get; set; }
    public bool? RecoverySuccess { get; set; }

    public string ChainId { get; set; }
    public ManagerInfo ManagerInfo { get; set; }
}
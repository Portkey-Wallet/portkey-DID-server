using System;
using System.Collections.Generic;
using CAServer.Account;
using CAServer.Hubs;

namespace CAServer.Dtos;

public class RecoveryDto: CAAccountBase
{
    public List<GuardianInfo> GuardianApproved { get; set; }
    public string LoginGuardianIdentifierHash { get; set; }
    public DateTime? RecoveryTime { get; set; }
    public bool? RecoverySuccess { get; set; }
    public string RecoveryMessage { get; set; }
    public HubRequestContext Context { get; set; }
}
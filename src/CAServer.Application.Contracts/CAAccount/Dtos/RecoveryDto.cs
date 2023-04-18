using System;
using System.Collections.Generic;
using CAServer.Account;
using CAServer.Hubs;

namespace CAServer.Dtos;

public class RecoveryDto: CAAccountBase
{
    public List<GuardianAccountInfo> GuardianApproved { get; set; }
    public string LoginGuardianAccount { get; set; }
    public DateTime? RecoveryTime { get; set; }
    public bool? RecoverySuccess { get; set; }
    public string RecoveryMessage { get; set; }
    public HubRequestContext Context { get; set; }
}
using System;
using System.Collections.Generic;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.Hubs;
using Orleans;

namespace CAServer.Dtos;

[GenerateSerializer]
public class RecoveryDto: CAAccountBase
{
    [Id(0)]
    public List<GuardianInfo> GuardianApproved { get; set; }

    [Id(1)]
    public string LoginGuardianIdentifierHash { get; set; }

    [Id(2)]
    public DateTime? RecoveryTime { get; set; }

    [Id(3)]
    public bool? RecoverySuccess { get; set; }

    [Id(4)]
    public string RecoveryMessage { get; set; }

    [Id(5)]
    public HubRequestContext Context { get; set; }

    [Id(6)]
    public ReferralInfo ReferralInfo { get; set; }
}
using System;
using System.Collections.Generic;
using CAServer.Account;
using CAServer.CAAccount;
using CAServer.CAAccount.Dtos;
using CAServer.Dtos;
using CAServer.Hubs;
using Volo.Abp.EventBus;

namespace CAServer.Etos;

[EventName("AccountRecoverCreateEto")]
public class AccountRecoverCreateEto : CAAccountBase
{
    public string GrainId { get; set; }
    public List<GuardianInfo> GuardianApproved { get; set; }
    public string LoginGuardianIdentifierHash { get; set; }
    public DateTime? RecoveryTime { get; set; }
    public bool? RecoverySuccess { get; set; }
    public string RecoveryMessage { get; set; }
    public HubRequestContext Context { get; set; }
    public ReferralInfo ReferralInfo { get; set; }
    public string IpAddress { get; set; }
}
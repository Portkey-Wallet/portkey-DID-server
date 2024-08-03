using System;
using CAServer.Account;
using CAServer.Dtos;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Dtos.Zklogin;
using CAServer.Hubs;
using Volo.Abp.EventBus;

namespace CAServer.Etos;

[EventName("AccountRegisterCreateEto")]
public class AccountRegisterCreateEto : CAAccountBase
{
    public string GrainId { get; set; }
    public GuardianInfo GuardianInfo { get; set; }
    public DateTime? RegisteredTime { get; set; }
    public bool? RegisterSuccess { get; set; }
    public string RegisterMessage { get; set; }
    public string RegisterStatus { get; set; }
    public HubRequestContext Context { get; set; }
    public ProjectDelegateInfo ProjectDelegateInfo { get; set; }
    public ReferralInfo ReferralInfo { get; set; }
    public string IpAddress { get; set; }
}
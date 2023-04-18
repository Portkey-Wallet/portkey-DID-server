using System;
using CAServer.Account;
using CAServer.Hubs;
using Volo.Abp.EventBus;

namespace CAServer.Etos;

[EventName("AccountRegisterCreateEto")]
public class AccountRegisterCreateEto : CAAccountBase
{
    public string GrainId { get; set; }
    public GuardianAccountInfo GuardianAccountInfo { get; set; }
    public DateTime? RegisteredTime { get; set; }
    public bool? RegisterSuccess { get; set; }
    public string RegisterMessage { get; set; }
    public string RegisterStatus { get; set; }
    public HubRequestContext Context { get; set; }
}
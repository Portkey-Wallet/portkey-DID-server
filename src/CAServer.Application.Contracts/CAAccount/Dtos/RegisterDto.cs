using System;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.Hubs;

namespace CAServer.Dtos;

public class RegisterDto : CAAccountBase
{
    public GuardianInfo GuardianInfo { get; set; }
    public DateTime? RegisteredTime { get; set; }
    public bool? RegisterSuccess { get; set; }
    public string RegisterMessage { get; set; }
    public HubRequestContext Context { get; set; }
    public ReferralInfo ReferralInfo { get; set; }
    public ProjectDelegateInfo ProjectDelegateInfo { get; set; }
}
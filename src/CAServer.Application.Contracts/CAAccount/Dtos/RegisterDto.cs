using System;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.Hubs;
using Orleans;

namespace CAServer.Dtos;

[GenerateSerializer]
public class RegisterDto : CAAccountBase
{
    [Id(0)]
    public GuardianInfo GuardianInfo { get; set; }
    
    [Id(1)]
    public DateTime? RegisteredTime { get; set; }
    
    [Id(2)]
    public bool? RegisterSuccess { get; set; }
    
    [Id(3)]
    public string RegisterMessage { get; set; }
    
    [Id(4)]
    public HubRequestContext Context { get; set; }
    
    [Id(5)]
    public ReferralInfo ReferralInfo { get; set; }
    
    [Id(6)]
    public ProjectDelegateInfo ProjectDelegateInfo { get; set; }
}
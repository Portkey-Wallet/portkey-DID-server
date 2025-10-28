using System;
using CAServer.CAAccount.Dtos;
using Orleans;
using Volo.Abp.EventBus;

namespace CAServer.ContractEventHandler;

[EventName("SocialRecoveryResultEvent")]
[GenerateSerializer]
public class SocialRecoveryEto : ContractServiceEto
{
    public SocialRecoveryEto()
    {
        RecoveryTime = DateTime.Now;
    }
    
    [Id(0)]
    public string GrainId { get; set; }

    [Id(1)]
    public Guid Id { get; set; }

    [Id(2)]
    public DateTime RecoveryTime { get; set; }

    [Id(3)]
    public string RecoveryMessage { get; set; }

    [Id(4)]
    public bool? RecoverySuccess { get; set; }

    [Id(5)]
    public ReferralInfo ReferralInfo { get; set; }

    [Id(6)]
    public string IpAddress { get; set; }
}
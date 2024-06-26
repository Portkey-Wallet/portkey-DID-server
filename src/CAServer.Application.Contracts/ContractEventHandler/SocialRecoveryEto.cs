using System;
using CAServer.CAAccount.Dtos;
using Volo.Abp.EventBus;

namespace CAServer.ContractEventHandler;

[EventName("SocialRecoveryResultEvent")]
public class SocialRecoveryEto : ContractServiceEto
{
    public SocialRecoveryEto()
    {
        RecoveryTime = DateTime.Now;
    }
    
    public string GrainId { get; set; }
    public Guid Id { get; set; }
    public DateTime RecoveryTime { get; set; }
    public string RecoveryMessage { get; set; }
    public bool? RecoverySuccess { get; set; }
    public ReferralInfo ReferralInfo { get; set; }
    public string IpAddress { get; set; }
}
using System;
using CAServer.CAAccount.Dtos;
using Volo.Abp.EventBus;

namespace CAServer.ContractEventHandler;

[EventName("CreateHolderResultEvent")]
public class CreateHolderEto : ContractServiceEto
{
    public CreateHolderEto()
    {
        RegisteredTime = DateTime.Now;
    }

    public Guid Id { get; set; }
    public string GrainId { get; set; }
    public DateTime RegisteredTime { get; set; }
    public string RegisterMessage { get; set; }
    public bool? RegisterSuccess { get; set; }
    public ReferralInfo ReferralInfo { get; set; }
    public string IpAddress { get; set; }
}
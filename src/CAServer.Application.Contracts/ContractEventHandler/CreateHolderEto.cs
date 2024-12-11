using System;
using CAServer.CAAccount.Dtos;
using Orleans;
using Volo.Abp.EventBus;

namespace CAServer.ContractEventHandler;

[EventName("CreateHolderResultEvent")]
[GenerateSerializer]
public class CreateHolderEto : ContractServiceEto
{
    public CreateHolderEto()
    {
        RegisteredTime = DateTime.Now;
    }

    [Id(0)]
    public Guid Id { get; set; }

    [Id(1)]
    public string GrainId { get; set; }

    [Id(2)]
    public DateTime RegisteredTime { get; set; }

    [Id(3)]
    public string RegisterMessage { get; set; }

    [Id(4)]
    public bool? RegisterSuccess { get; set; }

    [Id(5)]
    public ReferralInfo ReferralInfo { get; set; }

    [Id(6)]
    public string IpAddress { get; set; }
}
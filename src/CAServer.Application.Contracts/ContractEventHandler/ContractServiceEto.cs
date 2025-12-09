using System;
using CAServer.Hubs;
using Orleans;

namespace CAServer.ContractEventHandler;

[GenerateSerializer]
public class ContractServiceEto
{
    [Id(0)]
    public Guid Id { get; set; }

    [Id(1)]
    public string CaHash { get; set; }

    [Id(2)]
    public string CaAddress { get; set; }

    [Id(3)]
    public HubRequestContext Context { get; set; }
}
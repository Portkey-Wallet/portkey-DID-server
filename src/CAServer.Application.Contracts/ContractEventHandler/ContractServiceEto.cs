using System;
using CAServer.Hubs;

namespace CAServer.ContractEventHandler;

public class ContractServiceEto
{
    public Guid Id { get; set; }

    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public HubRequestContext Context { get; set; }
}
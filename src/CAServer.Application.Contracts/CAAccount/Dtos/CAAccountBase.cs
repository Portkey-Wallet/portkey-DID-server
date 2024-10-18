using System;
using Orleans;

namespace CAServer.Account;

[GenerateSerializer]
public class CAAccountBase
{
    [Id(0)]
    public Guid Id { get; set; }

    [Id(1)]
    public DateTime? CreateTime { get; set; }

    [Id(2)]
    public string ChainId { get; set; }

    [Id(3)]
    public ManagerInfo ManagerInfo { get; set; }

    [Id(4)]
    public string CaHash { get; set; }

    [Id(5)]
    public string CaAddress { get; set; }
}
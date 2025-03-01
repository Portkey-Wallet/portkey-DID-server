using System;
using CAServer.Commons.Etos;
using Orleans;

namespace CAServer.Chain;

[GenerateSerializer]
public class ChainDto : ChainDisplayNameDto
{
    [Id(0)]
    public string ChainId { get; set; }

    [Id(1)]
    public string ChainName { get; set; }

    [Id(2)]
    public string EndPoint { get; set; }

    [Id(3)]
    public string ExplorerUrl { get; set; }

    [Id(4)]
    public string CaContractAddress { get; set; }

    [Id(5)]
    public DateTime LastModifyTime { get; set; }

    [Id(6)]
    public DefaultToken DefaultToken { get; set; }
}

[GenerateSerializer]
public class DefaultToken
{
    [Id(0)]
    public string Name { get; set; }
    [Id(1)]
    public string Address { get; set; }
    [Id(2)]
    public string ImageUrl { get; set; }
    [Id(3)]
    public string Symbol { get; set; }
    [Id(4)]
    public string Decimals { get; set; }
    [Id(5)]
    public long IssueChainId { get; set; }
}
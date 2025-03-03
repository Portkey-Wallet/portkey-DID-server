using System;
using Orleans;

namespace CAServer.Growth.Dtos;

[GenerateSerializer]
public class GrowthBase
{
    [Id(0)]
    public string Id { get; set; }

    [Id(1)]
    public Guid UserId { get; set; }

    [Id(2)]
    public string CaHash { get; set; }

    [Id(3)]
    public string InviteCode { get; set; }

    [Id(4)]
    public string ReferralCode { get; set; }

    [Id(5)]
    public string ProjectCode { get; set; }

    [Id(6)]
    public string ShortLinkCode { get; set; }

    [Id(7)]
    public DateTime CreateTime { get; set; }
}
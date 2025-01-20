using System;
using CAServer.EnumType;
using Orleans;

namespace CAServer.RedDot.Dtos;

[GenerateSerializer]
public class RedDotInfo
{
    [Id(0)]
    public RedDotType RedDotType { get; set; }
    [Id(1)]
    public RedDotStatus Status { get; set; }
    [Id(2)]
    public DateTime CreateTime { get; set; }
    [Id(3)]
    public DateTime ReadTime { get; set; }
}
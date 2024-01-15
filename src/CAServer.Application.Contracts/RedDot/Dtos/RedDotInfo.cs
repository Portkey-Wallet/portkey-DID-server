using System;
using CAServer.EnumType;

namespace CAServer.RedDot.Dtos;

public class RedDotInfo
{
    public RedDotType RedDotType { get; set; }
    public RedDotStatus Status { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime ReadTime { get; set; }
}
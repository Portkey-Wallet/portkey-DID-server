using System;
using System.Collections.Generic;
using CAServer.RedDot.Dtos;

namespace CAServer.RedDot.Etos;

public class RedDotEto
{
    public string Id { get; set; }
    public Guid UserId { get; set; }
    public List<RedDotInfo> RedDotInfos { get; set; } = new();
}
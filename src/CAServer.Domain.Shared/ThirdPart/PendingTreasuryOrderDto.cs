using System;
using System.Collections.Generic;
using CAServer.Tokens;
using Orleans;

namespace CAServer.ThirdPart;

[GenerateSerializer]
public class PendingTreasuryOrderDto
{

    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string ThirdPartName { get; set; }
    [Id(2)]
    public string ThirdPartOrderId { get; set; }
    [Id(3)]
    public long LastModifyTime { get; set; }
    [Id(4)]
    public long ExpireTime { get; set; }
    [Id(5)]
    public long CreateTime { get; set; }
    [Id(6)]
    public string Status { get; set; }

    [Id(7)]
    public TreasuryOrderRequest TreasuryOrderRequest { get; set; }
    [Id(8)]
    public TokenExchange TokenExchange { get; set; }
    [Id(9)]
    public List<FeeItem> FeeInfo { get; set; }

}
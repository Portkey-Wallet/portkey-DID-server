using System;
using System.Collections.Generic;
using CAServer.Tokens;

namespace CAServer.ThirdPart;

public class PendingTreasuryOrderDto
{
    
    public Guid Id { get; set; }
    public string ThirdPartName { get; set; }
    public string ThirdPartOrderId { get; set; }
    public long LastModifyTime { get; set; }
    public long ExpireTime { get; set; }
    public long CreateTime { get; set; }
    public string Status { get; set; }
    
    public TreasuryOrderRequest TreasuryOrderRequest { get; set; }
    public TokenExchange TokenExchange { get; set; }
    public List<FeeItem> FeeInfo { get; set; }
    
}
using System;
using System.Collections.Generic;

namespace CAServer.RedPackage.Dtos;

public class RedPackageDetailDto
{
    public Guid Id { get; set; }
    public int TotalCount { get; set; }
    public string TotalAmount { get; set; }
    public string GrabbedAmount { get; set; }
    public string MinAmount { get; set; }
    public string CurrentUserGrabbedAmount { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string ChainId { get; set; }
    public string PublicKey { get; set; } = string.Empty;
    public Guid SenderId { get; set; }
    public Guid LuckKingId { get; set; } = Guid.Empty;
    public bool IsRedPackageFullyClaimed { get; set; }
    public bool IsRedPackageExpired { get; set; }
    public string SenderAvatar { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public long CreateTime { get; set; }
    public long EndTime { get; set; }
    public long ExpireTime { get; set; }
    public string Symbol { get; set; }
    public int Decimal { get; set; }
    public int Count { get; set; }
    public int Grabbed { get; set; }
    public string ChannelUuid { get; set; }
    public bool IsCurrentUserGrabbed { get; set; } = false;
    public RedPackageType Type { get; set; }
    public RedPackageStatus Status { get; set; }
    public List<GrabItemDto> Items { get; set; }
    
    public bool IfRefund{ get; set; }
}

public class GrabItemDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; }
    public string Avatar { get; set; }
    public long GrabTime { get; set; }
    public bool IsLuckyKing { get; set; }
    public string Amount { get; set; }
    
    public string CaAddress { get; set; }

    public bool PaymentCompleted;
}
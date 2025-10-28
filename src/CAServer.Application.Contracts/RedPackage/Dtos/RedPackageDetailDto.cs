using System;
using System.Collections.Generic;
using CAServer.Commons.Etos;
using CAServer.EnumType;
using Orleans;

namespace CAServer.RedPackage.Dtos;
[Serializable]
[GenerateSerializer]
public class RedPackageDetailDto
{
    [Id(0)]
    public Guid SessionId { get; set; }
    [Id(1)]
    public RedPackageDisplayType RedPackageDisplayType { get; set; }
    [Id(2)]
    public bool IsNewUsersOnly { get; set; }
    [Id(3)]
    public Guid Id { get; set; }
    [Id(4)]
    public int TotalCount { get; set; }
    [Id(5)]
    public string TotalAmount { get; set; }
    [Id(6)]
    public string GrabbedAmount { get; set; }
    [Id(7)]
    public string MinAmount { get; set; }
    [Id(8)]
    public string CurrentUserGrabbedAmount { get; set; } = "0";
    [Id(9)]
    public string Memo { get; set; } = string.Empty;
    [Id(10)]
    public string ChainId { get; set; }
    [Id(11)]
    public string PublicKey { get; set; } = string.Empty;
    [Id(12)]
    public Guid SenderId { get; set; }
    [Id(13)]
    public Guid LuckKingId { get; set; } = Guid.Empty;
    [Id(14)]
    public bool IsRedPackageFullyClaimed { get; set; }
    [Id(15)]
    public bool IsRedPackageExpired { get; set; }
    [Id(16)]
    public string SenderAvatar { get; set; } = string.Empty;
    [Id(17)]
    public string SenderName { get; set; } = string.Empty;
    [Id(18)]
    public long CreateTime { get; set; }
    [Id(19)]
    public long EndTime { get; set; }
    [Id(20)]
    public long ExpireTime { get; set; }
    [Id(21)]
    
    public long GrabExpireSeconds { get; set; }
    [Id(22)]
    public string Symbol { get; set; }
    [Id(23)]
    public int Decimal { get; set; }
    [Id(24)]
    public int Count { get; set; }
    [Id(25)]
    public int Grabbed { get; set; }
    [Id(26)]
    public string ChannelUuid { get; set; }
    [Id(27)]
    public bool IsCurrentUserGrabbed { get; set; } = false;
    [Id(28)]
    public RedPackageType Type { get; set; }
    [Id(29)]
    public string DisplayStatus { get; set; }
    [Id(30)]
    public RedPackageStatus Status { get; set; }
    [Id(31)]
    public List<GrabItemDto> Items { get; set; }
    [Id(32)]
    public bool IfRefund{ get; set; }
    [Id(33)]
    public int AssetType { get; set; }
    [Id(34)]
    public string Alias { get; set; }
    [Id(35)]
    public string TokenId { get; set; }
    [Id(36)]
    public string ImageUrl { get; set; }
    [Id(37)]
    public bool IsSeed { get; set; }
    [Id(38)]
    public int SeedType { get; set; }
    [Id(39)]
    public List<BucketItemDto> BucketNotClaimed { get; set; }
    [Id(40)]
    public List<BucketItemDto> BucketClaimed { get; set; }
}

[GenerateSerializer]
public class BucketItemDto
{
    [Id(0)]
    public int Index { get; set; }
    [Id(1)]
    public long Amount { get; set; }
    [Id(2)]
    public bool IsLuckyKing { get; set; }
    [Id(3)]
    public Guid UserId { get; set; }
}

[GenerateSerializer]
public class GrabItemDto
{
    [Id(0)]
    public Guid UserId { get; set; }
    [Id(1)]
    public string CaAddress { get; set; } = string.Empty;
    [Id(2)]
    public string Username { get; set; }
    [Id(3)]
    public string Avatar { get; set; }
    [Id(4)]
    public long GrabTime { get; set; }
    [Id(5)]
    public bool IsLuckyKing { get; set; }
    [Id(6)]
    public string Amount { get; set; }
    [Id(7)]
    public int Decimal { get; set; }
    [Id(8)]
    public bool PaymentCompleted;
    [Id(9)]
    public bool IsMe { get; set; }
    [Id(10)]
    public CryptoGiftDisplayType DisplayType { get; set; }
    [Id(11)]
    public long ExpirationTime { get; set; }
    [Id(12)]
    public string IpAddress { get; set; }
    [Id(13)]
    public string Identity { get; set; }
}
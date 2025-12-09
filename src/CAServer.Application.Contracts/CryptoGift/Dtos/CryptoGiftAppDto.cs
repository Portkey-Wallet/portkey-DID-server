using System;
using System.Collections.Generic;
using CAServer.RedPackage.Dtos;

namespace CAServer.CryptoGift.Dtos;

public class CryptoGiftAppDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public long TotalAmount { get; set; }
    public long PreGrabbedAmount { get; set; }
    public long CreateTime { get; set; }
    public string Symbol { get; set; }
    public List<PreGrabbedItemDto> Items { get; set; }

    public List<PreGrabBucketItemAppDto> BucketNotClaimed { get; set; }

    public List<PreGrabBucketItemAppDto> BucketClaimed { get; set; }
}

public class PreGrabBucketItemAppDto
{
    public int Index { get; set; }
    public long Amount { get; set; }
    public Guid UserId { get; set; }
    public string IdentityCode { get; set; }
}
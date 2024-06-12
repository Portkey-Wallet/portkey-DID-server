using System;

namespace CAServer.RedPackage.Dtos;

public class CryptoGiftHistoryItemDto
{
    public bool Exist { get; set; }
    public Guid Id { get; set; }
    public string TotalAmount { get; set; }
    public string GrabbedAmount { get; set; }
    public string Memo { get; set; } = string.Empty;
    public long CreateTime { get; set; }
    public string Status { get; set; }
    
    public string Symbol { get; set; }
    
    public int Decimal { get; set; }
    
    public RedPackageStatus RedPackageStatus { get; set; }
}
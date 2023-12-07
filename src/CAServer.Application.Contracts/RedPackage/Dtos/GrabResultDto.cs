namespace CAServer.RedPackage.Dtos;

public class GrabResultDto
{
    public RedPackageGrabStatus Result { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string Amount { get; set; }
    public int Decimal { get; set; }
    public RedPackageStatus Status { get; set; }
    
    public long ExpireTime { get; set; }
}
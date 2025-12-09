namespace CAServer.RedPackage.Dtos;

public class GrabRedPackageOutputDto
{
    public RedPackageGrabStatus Result { get; set; }
    public string Amount { get; set; }
    public int Decimal { get; set; }
    public int ErrorCode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public RedPackageStatus Status { get; set; }
}
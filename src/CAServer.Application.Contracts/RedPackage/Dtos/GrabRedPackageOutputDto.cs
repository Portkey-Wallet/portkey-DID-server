namespace CAServer.RedPackage.Dtos;

public class GrabRedPackageOutputDto
{
    public RedPackageGrabStatus Result { get; set; }
    public decimal Amount { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public RedPackageStatus Status { get; set; }
}
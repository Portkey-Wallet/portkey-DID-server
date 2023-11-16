namespace CAServer.RedPackage.Dtos;

public class GrabResultDto
{
    public RedPackageGrabStatus Result { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public RedPackageStatus Status { get; set; }
}
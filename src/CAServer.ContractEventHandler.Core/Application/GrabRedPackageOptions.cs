namespace CAServer.ContractEventHandler.Core.Application;

public class GrabRedPackageOptions
{
    public int Interval { get; set; } = 15;
    public string Corn { get; set; } = "0/9 * * * * ?";
}
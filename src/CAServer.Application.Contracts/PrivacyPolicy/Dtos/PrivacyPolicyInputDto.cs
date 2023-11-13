namespace CAServer.PrivacyPolicy.Dtos;

public class PrivacyPolicyInputDto
{
    public int PolicyVersion { get; set; }
    public string CaHash { get; set; }
    public string Origin { get; set; }
    public int Scene { get; set; }
    public string ManagerAddress { get; set; }
    public long Timestamp { get; set; }
    public string PolicyId { get; set; }
}
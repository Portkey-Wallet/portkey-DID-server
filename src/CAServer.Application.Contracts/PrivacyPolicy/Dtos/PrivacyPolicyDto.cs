namespace CAServer.PrivacyPolicy.Dtos;

public class PrivacyPolicyDto
{
    public string Id { get; set; }
    public int PolicyVersion { get; set; }
    public string CaHash { get; set; }
    public string Origin { get; set; }
    public int Scene { get; set; }
    public string ManagerAddress { get; set; }
    public string PolicyId { get; set; }
    public long Timestamp { get; set; }
    public string Content { get; set; }
}
namespace CAServer.Security.Dtos;

public class UserSecuritySelfTestResultDto
{
    public bool SocialRecovery { get; set; }
    public bool ModifyGuardian { get; set; }
    public bool RemoveDevice { get; set; }
    public bool ModifyTransferLimit { get; set; }
    public bool Approve { get; set; }
    public bool ModifyStrategy { get; set; }
}
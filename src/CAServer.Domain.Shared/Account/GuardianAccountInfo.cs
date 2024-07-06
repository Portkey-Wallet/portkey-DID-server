using CAServer.CAAccount.Dtos.Zklogin;

namespace CAServer.Account;

public class GuardianInfo
{
    public string IdentifierHash { get; set; }
    public GuardianType Type { get; set; }
    public VerificationInfo VerificationInfo { get; set; }
    
    public ZkJwtAuthInfoDto ZkJwtAuthInfo { get; set; }
}

public class VerificationInfo
{
    public string Id { get; set; }
    public string VerificationDoc { get; set; }
    public string Signature { get; set; }
}
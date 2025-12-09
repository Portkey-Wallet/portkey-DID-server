using CAServer.CAAccount.Dtos.Zklogin;
using Orleans;

namespace CAServer.Account;

[GenerateSerializer]
public class GuardianInfo
{
    [Id(0)]
    public string IdentifierHash { get; set; }
    
    [Id(1)]
    public GuardianType Type { get; set; }
    
    [Id(2)]
    public VerificationInfo VerificationInfo { get; set; }
    
    [Id(3)]
    public ZkLoginInfoDto ZkLoginInfo { get; set; }
}

[GenerateSerializer]
public class VerificationInfo
{
    
    [Id(0)]
    public string Id { get; set; }
    
    [Id(1)]
    public string VerificationDoc { get; set; }
    
    [Id(2)]
    public string Signature { get; set; }
}
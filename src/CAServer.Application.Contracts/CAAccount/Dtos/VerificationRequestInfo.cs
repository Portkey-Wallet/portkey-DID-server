using CAServer.CAAccount.Enums;

namespace CAServer.CAAccount.Dtos;

public class VerificationRequestInfo
{
    public VerifierType VerifierType { get; set; }
    
    public VerificationDetails VerificationDetails { get; set; }
}
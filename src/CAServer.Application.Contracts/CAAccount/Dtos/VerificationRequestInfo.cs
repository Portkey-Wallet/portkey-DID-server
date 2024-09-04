using CAServer.CAAccount.Enums;

namespace CAServer.CAAccount.Dtos;

public class VerificationRequestInfo
{
    public VerificationType VerificationType { get; set; }
    
    public VerificationDetails VerificationDetails { get; set; }
}
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Enums;

namespace CAServer.Account;

public class VerificationDo
{
    public VerificationType VerificationType { get; set; }
    
    public VerificationDetails VerificationDetails { get; set; }
}
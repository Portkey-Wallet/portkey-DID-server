using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Enums;

namespace CAServer.Account;

public class VerificationDo
{
    public VerifierType VerifierType { get; set; }
    
    public VerificationDetails VerificationDetails { get; set; }
}
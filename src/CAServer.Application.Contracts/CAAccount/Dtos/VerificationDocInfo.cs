using AElf.Types;

namespace CAServer.CAAccount.Dtos;

public class VerificationDocInfo
{
    public string GuardianType { get; set; }
    public Hash IdentifierHash { get; set; }
    public string VerificationTime { get; set; }
    public Address VerifierAddress { get; set; }
    public string Salt { get; set; }
    public string OperationType { get; set; }
}
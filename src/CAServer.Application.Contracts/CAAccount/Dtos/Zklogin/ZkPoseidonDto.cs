using Portkey.Contracts.CA;

namespace CAServer.CAAccount.Dtos.Zklogin;

public class ZkPoseidonDto
{
    public string ChainId { get; set; }
    public string CaHash { get; set; }
    public GuardianType GuardianType { get; set; }
    public string GuardianIdentifier { get; set; }
    public string IdentifierHash { get; set; }
    public string Salt { get; set; }
    public string PoseidonHash { get; set; }
    public string ErrorMessage { get; set; }
}
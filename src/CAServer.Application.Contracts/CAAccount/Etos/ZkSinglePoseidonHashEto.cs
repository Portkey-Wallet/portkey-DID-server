using CAServer.CAAccount.Dtos;

namespace CAServer.Etos;

public class ZkSinglePoseidonHashEto
{
    public string CaHash { get; set; }
    public GuardianIdentifierType Type { get; set; }
    public string IdentifierHash { get; set; }
    public string PoseidonIdentifierHash { get; set; }
}
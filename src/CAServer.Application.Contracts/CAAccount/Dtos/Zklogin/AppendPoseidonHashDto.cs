using Portkey.Contracts.CA;

namespace CAServer.CAAccount.Dtos.Zklogin;

public class AppendPoseidonHashDto
{
    public string ChainId { get; set; }
    public AppendGuardianInput Input { get; set; }
}
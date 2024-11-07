using CAServer.Commons.Etos;
using Portkey.Contracts.CA;

namespace CAServer.CAAccount.Dtos.Zklogin;

public class AppendPoseidonHashDto : ChainDisplayNameDto
{
    public AppendGuardianInput Input { get; set; }
}
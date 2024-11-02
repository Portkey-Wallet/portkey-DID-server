using CAServer.Commons.Etos;

namespace CAServer.CAAccount.Dtos;

public class ManagerCacheDto : ChainDisplayNameDto
{
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
}
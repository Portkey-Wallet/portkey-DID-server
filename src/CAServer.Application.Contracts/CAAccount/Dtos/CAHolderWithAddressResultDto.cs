using CAServer.Dtos;

namespace CAServer.CAAccount.Dtos;

public class CAHolderWithAddressResultDto : CAHolderDto
{
    public string CaAddress { get; set; }
}
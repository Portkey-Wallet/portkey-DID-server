using System.Collections.Generic;
using CAServer.Dtos;

namespace CAServer.CAAccount.Dtos;

public class CAHolderReponse
{
    public List<CAHolderResultDto> CaHolders { get; set; }
    
    public long Total { get; set; }
}
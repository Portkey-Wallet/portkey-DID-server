using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace CAServer.CAActivity.Dtos;

public class GetTwoCaTransactionRequestDto : PagedResultRequestDto
{
    public List<string> CaAddresses { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    
    public int Width { get; set; }

    public int Height { get; set; }
}
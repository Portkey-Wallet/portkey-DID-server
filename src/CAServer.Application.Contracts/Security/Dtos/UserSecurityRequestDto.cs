using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace CAServer.Security.Dtos;

public class GetTransferLimitListByCaHashDto : PagedResultRequestDto
{
    [Required] public string CaHash { get; set; }
}

public class GetManagerApprovedListByCaHashDto : PagedResultRequestDto
{
    [Required] public string ChainId { get; set; }
    [Required] public string CaHash { get; set; }
    public string Spender { get; set; }
    public string Symbol { get; set; }
}

public class GetTokenBalanceTransferCheckDto
{
    [Required] public string CaHash { get; set; }
}

public class GetTokenBalanceTransferCheckWithChainIdDto
{
    [Required] public string CaHash { get; set; }
    public string CheckTransferSafeChainId { get; set; }
}
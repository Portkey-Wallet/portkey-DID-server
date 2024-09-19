using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace CAServer.Tokens.Dtos;

public class GetTokenListV2RequestDto : PagedResultRequestDto
{
    [Required] public string Symbol { get; set; }
}
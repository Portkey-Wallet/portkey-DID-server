using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace CAServer.Tokens.Dtos;

public class GetTokenInfosRequestDto : PagedResultRequestDto
{
    public string Keyword { get; set; }
    public List<string> ChainIds { get; set; }
}
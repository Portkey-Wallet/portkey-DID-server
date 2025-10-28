using Volo.Abp.Application.Dtos;

namespace CAServer.Tokens.Dtos;

public class GetTokenInfosV2RequestDto: PagedResultRequestDto
{
    public string Keyword { get; set; }
}
using Volo.Abp.Application.Dtos;

namespace CAServer.Tokens.Dtos;

public class GetUserTokenListInput : PagedResultRequestDto
{
    public string Filter { get; set; }
    public bool IsDisplay { get; set; }

}
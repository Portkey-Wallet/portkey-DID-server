using Volo.Abp.Application.Dtos;

namespace CAServer.Contacts;

public class ContactGetListDto : PagedResultRequestDto
{
    public string Filter { get; set; }
    public string Sort {get; set; }
}
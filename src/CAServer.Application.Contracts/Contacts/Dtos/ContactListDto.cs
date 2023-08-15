using Volo.Abp.Application.Dtos;

namespace CAServer.Contacts;

public class ContactListDto : PagedResultRequestDto
{
    public string KeyWord { get; set; }
}
using System;
using Volo.Abp.Application.Dtos;

namespace CAServer.Contacts;

public class ContactListDto : PagedResultRequestDto
{
    public string KeyWord { get; set; }
    public Guid UserId { get; set; }
    
    public int TabType { get; set; } = 0;
}
using System;
using Volo.Abp.Application.Dtos;

namespace CAServer.Contacts;

public class ContactGetListDto : PagedResultRequestDto
{
    public string KeyWord { get; set; }
    public Guid UserId { get; set; }
    
    public bool IsAbleChat { get; set; }
    
    public long ModificationTime { get; set; }
    
    
}
using System;

namespace CAServer.Contacts;

public class ContactProfileRequestDto
{
    public Guid UserId { get; set; }
    
    public string RelationId { get; set; }
    
}
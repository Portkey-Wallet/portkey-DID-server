using System;
using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace CAServer.Etos;

[EventName("ContactUpdateEto")]
public class ContactUpdateEto
{
    public Guid Id { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public List<ContactAddressEto> Addresses { get; set; } = new();
    public Guid UserId { get; set; }
    public bool IsDeleted { get; set; } = true;
    public DateTime ModificationTime { get; set; }
}
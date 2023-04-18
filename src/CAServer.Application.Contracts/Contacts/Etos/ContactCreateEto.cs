using System;
using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace CAServer.Etos;

[EventName("ContactCreateEto")]
public class ContactCreateEto
{
    public Guid Id { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public List<ContactAddressEto> Addresses { get; set; } = new();
    public Guid UserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime ModificationTime { get; set; }
}
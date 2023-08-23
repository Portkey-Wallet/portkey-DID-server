using System;
using System.Collections.Generic;
using CAServer.Contacts;
using Volo.Abp.EventBus;

namespace CAServer.Etos;

[EventName("ContactUpdateEto")]
public class ContactUpdateEto
{
    public Guid Id { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
    public List<ContactAddressEto> Addresses { get; set; } = new();
    public Guid UserId { get; set; }
    public CaHolderInfo CaHolderInfo { get; set; }
    public ImInfo ImInfo { get; set; }
    public bool IsDeleted { get; set; } = true;
    public bool IsImputation { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime ModificationTime { get; set; }
}
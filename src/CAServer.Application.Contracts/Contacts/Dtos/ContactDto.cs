using System;
using System.Collections.Generic;

namespace CAServer.Contacts;

public class ContactDto
{
    public Guid Id { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public List<ContactAddressDto> Addresses { get; set; } = new();
    public Guid UserId { get; set; }
    public CaHolderInfoDto CaHolderInfo { get; set; }
    public ImInfoDto ImInfo { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreateTime { get; set; }
    public long ModificationTime { get; set; }
    public string Avatar { get; set; }
    public bool IsImputation { get; set; }
}
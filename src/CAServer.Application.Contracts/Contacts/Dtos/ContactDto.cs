using System;
using System.Collections.Generic;

namespace CAServer.Contacts;

public class ContactDto
{
    public Guid Id { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
    public List<ContactAddressDto> Addresses { get; set; } = new();
    public Guid UserId { get; set; }
    public CaHolderInfo CaHolderInfo { get; set; }
    public ImInfo ImInfo { get; set; }
    public bool IsDeleted { get; set; } = true;
    public bool IsImputation { get; set; }
    public DateTime CreateTime { get; set; }
    public long ModificationTime { get; set; }
}

public class CaHolderInfo
{
    public Guid UserId { get; set; }
    public string CaHash { get; set; }
    public string WalletName { get; set; }
}

public class ImInfo
{
    public string RelationId { get; set; }
    public Guid PortkeyId { get; set; }
    public string Name { get; set; }
}
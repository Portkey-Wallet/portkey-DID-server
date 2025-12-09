using System;
using System.Collections.Generic;

namespace CAServer.Contacts;

public class ContactListDto
{
    public string Id { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
    public List<ContactAddressDto> Addresses { get; set; } = new();
    public string UserId { get; set; }
    public CaHolderInfo CaHolderInfo { get; set; }
    public ImInfos ImInfo { get; set; }
    public bool IsDeleted { get; set; } = true;
    public bool IsImputation { get; set; }
    public DateTime CreateTime { get; set; }
    public long ModificationTime { get; set; }
    public int ContactType { get; set; }
}

public class CaHolderDto
{
    public string UserId { get; set; }
    public string CaHash { get; set; }
    public string WalletName { get; set; }
}

public class ImInfos
{
    public string RelationId { get; set; }
    public string PortkeyId { get; set; }
    public string Name { get; set; }
}
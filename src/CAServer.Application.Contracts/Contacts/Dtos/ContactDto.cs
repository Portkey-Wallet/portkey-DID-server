using System;
using System.Collections.Generic;
using Nest;
using Orleans;

namespace CAServer.Contacts;

[GenerateSerializer]
public class ContactDto
{
    [Id(0)]
    public Guid Id { get; set; }

    [Id(1)]
    public string Index { get; set; }

    [Id(2)]
    public string Name { get; set; }

    [Id(3)]
    public string Avatar { get; set; }

    [Id(4)]
    public List<ContactAddressDto> Addresses { get; set; } = new();

    [Id(5)]
    public Guid UserId { get; set; }

    [Id(6)]
    public CaHolderInfo CaHolderInfo { get; set; }

    [Id(7)]
    public ImInfo ImInfo { get; set; }

    [Id(8)]
    public bool IsDeleted { get; set; } = true;

    [Id(9)]
    public bool IsImputation { get; set; }

    [Id(10)]
    public DateTime CreateTime { get; set; }

    [Id(11)]
    public long ModificationTime { get; set; }

    [Id(12)]
    public int ContactType { get; set; }
}

[GenerateSerializer]
public class CaHolderInfo
{
    [Id(0)]
    public Guid UserId { get; set; }
    
    [Id(1)]
    public string CaHash { get; set; }
    
    [Id(2)]
    public string WalletName { get; set; }
}

[GenerateSerializer]
public class ImInfo
{
    [Id(0)]
    public string RelationId { get; set; }
    
    [Id(1)]
    public Guid PortkeyId { get; set; }
    
    [Id(2)]
    public string Name { get; set; }
}

public class HolderInfoWithAvatar
{
    public Guid UserId { get; set; }
    public string CaHash { get; set; }
    public string WalletName { get; set; }
    public string Avatar { get; set; }
}
using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class ContactIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public override Guid Id { get; set; }
    [Keyword] public string Index { get; set; }
    [Wildcard] public string Name { get; set; }
    public string Avatar { get; set; }
    public List<ContactAddress> Addresses { get; set; } = new();
    [Keyword] public Guid UserId { get; set; }
    public CaHolderInfo CaHolderInfo { get; set; }
    public ImInfo ImInfo { get; set; }
    [Keyword] public bool IsDeleted { get; set; } = true;
    [Keyword] public bool IsImputation { get; set; }
    [Keyword] public DateTime CreateTime { get; set; }
    [Keyword] public DateTime ModificationTime { get; set; }

    public int ContactType { get; set; } = 0;
}

public class ContactAddress
{
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string ChainName { get; set; }
    [Keyword] public string Address { get; set; }
}

public class CaHolderInfo
{
    [Keyword] public Guid UserId { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Wildcard] public string WalletName { get; set; }
}

public class ImInfo
{
    [Keyword] public string RelationId { get; set; }
    [Keyword] public string PortkeyId { get; set; }
    [Wildcard] public string Name { get; set; }
}
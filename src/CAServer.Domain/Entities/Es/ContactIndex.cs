using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class ContactIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public string Index { get; set; }
    [Keyword] public string Name { get; set; }
    public List<ContactAddress> Addresses { get; set; } = new();
    [Keyword] public Guid UserId { get; set; }
    [Keyword] public bool IsDeleted { get; set; } = true;
    [Keyword] public DateTime ModificationTime { get; set; }
}

public class ContactAddress
{
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
}
using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class AddressBookMigrateIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public Guid OriginalContactId { get; set; }
    [Keyword] public Guid NewContactId { get; set; }
    [Keyword] public Guid UserId { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string FailType { get; set; }
    [Keyword] public string Status { get; set; }
    [Keyword] public string Message { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}